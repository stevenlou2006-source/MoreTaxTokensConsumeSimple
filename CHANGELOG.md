# 更新日志

## [1.2.4] - 2026-05-25
### 适配
- 适配游戏 v0.4.4

## [1.2.3] - 2026-05-15
### 文档
- 修正文本说明。

## [1.2.2] - 2026-05-12
### 适配
- 适配游戏 v0.4.2

## [1.2.1] - 2026-05-10
### 修复
- 批量抽奖状态机完全隔离至主机端 (IsMasterClient)，非主机客户端不再维护批量队列，仅发送单次原版 InteractClientRPC。
- 移除所有直接 PhotonNetwork 引用，改用反射访问 IsMasterClient，避免缺少 DLL 引用编译错误。
- 细化 Unload 清理逻辑，确保批量流程在场景切换时完全重置。

## [1.2.0] - 2026-05-10
### 重构
- 彻底重写批量流程，放弃循环调用 `Interact` 和协程等待方案。
- 引入三态锁 (`_batchInProgress` / `_isProcessing` / `_delayedFrameCount`) 彻底解决死循环。
- 采用帧延迟 (3 帧) 配合状态机 Idle 监听，实现稳定“自动连抽”。
- 补丁点从 `MetaManager.CosmeticTokenRemove` 切换至 `CosmeticShopMachine.Interact` + `UpdateState`。

## [1.1.1] - 2026-05-10
### 修复
- 移除协程宿主依赖，改用 MonoBehaviour.Update 帧计数延迟，修复 NullReferenceException 崩溃。
- 添加 `_isTriggering` 锁，防止同一帧内多次触发。

## [1.1.0] - 2026-05-10
### 重构
- 放弃循环调用 `Interact`，改用状态机监听 + 协程延迟触发。
- 补丁点同时挂载 `CosmeticShopMachine.Interact` (入口) 和 `UpdateState` (状态监听)。
- 批量流程改为：手动触发一次，后续自动等待 Idle 后依次触发。

## [1.0.3] - 2026-05-10
### 修复
- 再次尝试循环调用 `CosmeticShopMachine.Interact`，补丁挂载至该方法。
- 仍然崩溃，记录为后续重构参考。

## [1.0.2] - 2026-05-10
### 修复
- 放弃循环调用 `MetaManager.CosmeticTokenRemove`，改为前缀追加代币方案，避免重入崩溃。
- 代币扣减正确但奖励次数仍为 1 次。

## [1.0.1] - 2026-05-10
### 修复
- 尝试循环调用 `CosmeticTokenRemove` 原方法，导致游戏崩溃。
- 记录崩溃日志，回退至 v1.0.0 方案。

## [1.0.0] - 2026-05-10
### 初始发布
- 拦截 `MetaManager.CosmeticTokenRemove`，通过前缀批量删除代币列表元素。
- 支持批量扣除代币数量，但奖励发放次数与扣除数量不一致。

## [0.1.0] - 2026-05-09
### 早期概念
- 首次构思批量消费代币功能。
- 使用反射探测 `MetaManager` 内可能的消费方法，为后续定位 `CosmeticTokenRemove` 打下基础。
- 配置项 TokenConsumeBatchSize 和 EnableBatchConsume 定义。