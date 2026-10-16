# More Tax Tokens Consume Simple

在商店抽奖机消费时批量扣除税款代币。

版本：1.2.4

## 概述
原版每次抽外观只扣 1 个代币。本模组允许一次批量扣除多个（默认 10 个），相当于一次抽多次。

搭配 MoreTaxTokensSimple（获取倍率）使用效果更佳——一个管获取，一个管消费。

> 特别感谢 **Omniscye (Empress)** 的代码审查和网络架构指导。

## ⚠️ 使用说明
- **仅主机/单人有效**。非主机客户端不触发批量抽奖，仅走原版单次交互。
- 本模组不自定义任何 Photon 事件，完全通过原版状态机驱动。

## ✨ 功能
- 自定义每次消费时代币扣除数量（默认 10，范围 1-100）
- 软兼容 REPOConfig，可在游戏内实时调整
- 轻量、无额外功能

## 安装
- 确保已安装 [BepInExPack](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)。
- 将 `MoreTaxTokensConsumeSimple.dll` 放入 `BepInEx/plugins` 文件夹。

## ⚙️ 配置
配置文件位置：`BepInEx/config/cn_xc.MoreTaxTokensConsumeSimple.cfg`

| 配置项 | 默认值 | 说明 |
|---|---|---|
| EnableBatchConsume | true | 是否启用批量消费 |
| TokenConsumeBatchSize | 10 | 每次消费扣除的代币数量（1-100） |

## 致谢
- 代码审查与网络架构指导：**Omniscye (Empress)**

## 模块问题交流
QQ群：573485890