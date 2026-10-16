using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace cn_xc.MoreTaxTokensConsumeSimple
{
	[BepInPlugin("cn_xc.MoreTaxTokensConsumeSimple", "More Tax Tokens Consume Simple", "1.2.1")]
	[BepInProcess("REPO.exe")]
	public class MoreTaxTokensConsumePlugin : BaseUnityPlugin
	{
		internal static ManualLogSource Log;
		public static ConfigEntry<int> TokenConsumeBatchSize;
		public static ConfigEntry<bool> EnableBatchConsume;

		private readonly Harmony harmony = new Harmony("cn_xc.MoreTaxTokensConsumeSimple");

		// 批量抽奖状态（仅主机端维护）
		private static MoreTaxTokensConsumePlugin _pluginInstance;
		private static int _remainingBatchPulls;
		private static object _currentMachineInstance;
		private static MethodInfo _interactMethod;
		private static bool _isProcessing;       // 防止状态机回调重入
		private static bool _batchInProgress;    // 是否处于批量流程
		private static int _delayedFrameCount;   // 延迟帧计数器

		private void Awake()
		{
			_pluginInstance = this;
			Log = Logger;

			EnableBatchConsume = Config.Bind("General", "EnableBatchConsume", true,
				"启用批量抽奖。仅主机/单人有效。");
			TokenConsumeBatchSize = Config.Bind("General", "TokenConsumeBatchSize", 10,
				new ConfigDescription("每次触发抽奖的次数 (1-100)。", new AcceptableValueRange<int>(1, 100)));

			var shopMachineType = AccessTools.TypeByName("CosmeticShopMachine");
			if (shopMachineType != null)
			{
				_interactMethod = AccessTools.Method(shopMachineType, "Interact");
				if (_interactMethod != null)
				{
					harmony.Patch(_interactMethod,
						prefix: new HarmonyMethod(typeof(MoreTaxTokensConsumePlugin), nameof(InteractPrefix)));
					Log.LogInfo("✅ 补丁成功: CosmeticShopMachine.Interact (批量抽奖入口)");
				}

				var updateStateMethod = AccessTools.Method(shopMachineType, "UpdateState");
				if (updateStateMethod != null)
				{
					harmony.Patch(updateStateMethod,
						postfix: new HarmonyMethod(typeof(MoreTaxTokensConsumePlugin), nameof(UpdateStatePostfix)));
					Log.LogInfo("✅ 补丁成功: CosmeticShopMachine.UpdateState (状态监听)");
				}
			}

			Log.LogInfo($"More Tax Tokens Consume Simple v1.2.1 初始化完成！TokenConsumeBatchSize = {TokenConsumeBatchSize.Value}");
		}

		/// <summary>
		/// 拦截CosmeticShopMachine.Interact，如果是主机/单人则启动批量流程
		/// </summary>
		public static void InteractPrefix(object __instance)
		{
			if (!EnableBatchConsume.Value) return;

			// 非主机客户端不执行任何批量逻辑，仅发送原版单次交互RPC
			if (SemiFunc.IsMultiplayer() && !PhotonNetwork.IsMasterClient) return;

			// 如果正在处理自动触发，直接放行
			if (_isProcessing) return;

			// 如果已经处在批量流程中，阻止外部手动交互
			if (_batchInProgress) return;

			int batchSize = TokenConsumeBatchSize.Value;
			if (batchSize <= 1) return;

			// 检查代币是否足够
			var metaManager = MetaManager.instance;
			if (metaManager == null) return;

			var tokensField = AccessTools.Field(typeof(MetaManager), "cosmeticTokens");
			var tokensList = tokensField?.GetValue(metaManager) as IList;
			if (tokensList == null || tokensList.Count < batchSize) return;

			// 启动批量流程（主机端）
			_batchInProgress = true;
			_remainingBatchPulls = batchSize - 1; // 第一次由玩家触发，剩余次数自动触发
			_currentMachineInstance = __instance;
			_delayedFrameCount = 0;
			_isProcessing = false;

			Log.LogInfo($"🎰 批量抽奖启动：总共 {batchSize} 次（第 1 次由玩家触发，剩余 {_remainingBatchPulls} 次自动触发）");
		}

		/// <summary>
		/// 监听CosmeticShopMachine.UpdateState，当机器回到Idle时准备下一次自动触发
		/// </summary>
		public static void UpdateStatePostfix(object __instance, object _state)
		{
			// 仅主机/单人维护批量状态
			if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

			if (!_batchInProgress) return;
			if (_remainingBatchPulls <= 0) return;
			if (_isProcessing) return;
			if (_currentMachineInstance == null || _interactMethod == null) return;
			if (__instance != _currentMachineInstance) return;

			string stateName = _state?.ToString();
			if (stateName != "Idle") return;

			// 再次检查代币
			var metaManager = MetaManager.instance;
			if (metaManager == null) return;
			var tokensField = AccessTools.Field(typeof(MetaManager), "cosmeticTokens");
			var tokensList = tokensField?.GetValue(metaManager) as IList;
			if (tokensList == null || tokensList.Count < 1)
			{
				CleanupBatch("代币不足");
				return;
			}

			// 设置延迟帧，让状态机完全稳定
			_delayedFrameCount = 3;
		}

		private void Update()
		{
			// 仅主机/单人处理延迟触发
			if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

			if (_delayedFrameCount <= 0) return;
			if (_isProcessing) return;
			if (!_batchInProgress) return;
			if (_remainingBatchPulls <= 0) return;

			_delayedFrameCount--;

			if (_delayedFrameCount == 0)
			{
				_isProcessing = true;
				try
				{
					if (_currentMachineInstance == null || _interactMethod == null)
					{
						CleanupBatch("机器实例丢失");
						return;
					}

					// 最后一刻检查代币
					var metaManager = MetaManager.instance;
					if (metaManager == null)
					{
						CleanupBatch("MetaManager丢失");
						return;
					}

					var tokensField = AccessTools.Field(typeof(MetaManager), "cosmeticTokens");
					var tokensList = tokensField?.GetValue(metaManager) as IList;
					if (tokensList == null || tokensList.Count < 1)
					{
						CleanupBatch("代币耗尽");
						return;
					}

					_remainingBatchPulls--;
					int currentPull = TokenConsumeBatchSize.Value - _remainingBatchPulls;
					Log.LogInfo($"🎰 自动触发第 {currentPull}/{TokenConsumeBatchSize.Value} 次抽奖 (剩余：{_remainingBatchPulls})");

					_interactMethod.Invoke(_currentMachineInstance, null);

					if (_remainingBatchPulls <= 0)
					{
						Log.LogInfo("🎰 批量抽奖完成！");
						CleanupBatch("正常结束");
					}
				}
				catch (Exception ex)
				{
					Log.LogError($"自动触发抽奖失败：{ex.Message}");
					CleanupBatch("异常");
				}
				finally
				{
					_isProcessing = false;
				}
			}
		}

		private static void CleanupBatch(string reason)
		{
			if (!string.IsNullOrEmpty(reason))
				Log.LogDebug($"批量抽奖清理：{reason}");
			_batchInProgress = false;
			_remainingBatchPulls = 0;
			_currentMachineInstance = null;
			_isProcessing = false;
			_delayedFrameCount = 0;
		}
	}
}