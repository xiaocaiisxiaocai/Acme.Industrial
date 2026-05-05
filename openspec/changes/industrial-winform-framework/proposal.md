# 工业级 WinForm 框架 - 提案

## Why

工业自动化领域需要一个**可扩展、可持续维护的 WinForm 应用框架**。当前工业软件存在以下问题：

- **重复建设**：每个项目重新封装 Modbus/S7 通讯代码
- **架构混乱**：业务代码与 UI 代码耦合，难以测试
- **扩展困难**：新增 PLC 型号需要修改大量业务代码
- **维护成本高**：代码质量参差不齐，新人上手困难

本框架旨在提供一套**统一的基础设施**，让开发团队专注于业务逻辑而非底层实现。

## What Changes

本项目将构建一套**分层架构的工业 WinForm 框架**，包含以下核心能力：

1. **Core 层** - 核心抽象与基础类型
   - 统一返回模型 (OperateResult)
   - 依赖注入容器
   - 事件总线
   - 日志、配置、缓存抽象

2. **Communication 层** - 通讯协议抽象
   - 统一设备驱动接口
   - 点位 (Tag) 模型与订阅
   - 多协议支持 (Modbus/S7/MC/OPC UA)
   - Mock 驱动 (测试用)

3. **Services 层** - 业务服务层
   - 数据采集服务
   - 报警服务
   - 历史数据服务
   - 配方管理

4. **UI 层** - 界面框架
   - MVP 模式基类
   - 工业控件库 (指示灯、仪表、趋势图)
   - HMI 组态系统

5. **Scripting 层** - 脚本引擎
   - 基于 Roslyn 的 C# 脚本执行
   - 点位访问能力注入
   - 定时/事件触发脚本

6. **预留扩展** - 运动控制与视觉
   - 运动控制器抽象
   - 工业相机接口

## Capabilities

### New Capabilities

- `core-abstractions` - 核心抽象层：OperateResult、错误码、模块/插件接口、DI容器抽象
- `communication-layer` - 通讯协议层：IDeviceDriver、Tag模型、驱动工厂、设备管理器
- `data-access` - 数据访问层：IRepository、UnitOfWork、SqlSugar实现
- `business-services` - 业务服务层：数据采集、报警、历史记录
- `ui-framework` - UI框架层：MVP基类、窗体模板、工业控件
- `hmi-system` - HMI组态系统：设计器、运行时、图元库
- `scripting-engine` - 脚本引擎：Roslyn C#执行、上下文注入
- `motion-vision` - 运动与视觉（预留）：控制器抽象、相机接口

### Modified Capabilities

无 - 全新项目

## Impact

### 影响的代码

- 新建 `Acme.Industrial.sln` 解决方案
- 新建 30+ 个项目 (Core/Communication/Services/UI/Modules/Scripting/Host/Tests)
- 预计代码量：50,000+ 行

### 依赖关系

- 框架层依赖：
  - Autofac (DI)
  - Serilog (日志)
  - SqlSugar (ORM)
  - Roslyn (脚本)
  - Newtonsoft.Json (序列化)

### 项目结构

```
Acme.Industrial/
├── 01-Core/              # 核心抽象
├── 02-Infrastructure/    # 基础设施
├── 03-Communication/     # 通讯协议
├── 04-Services/          # 业务服务
├── 05-UI/               # 界面框架
├── 06-Modules/           # 业务模块
├── 07-Plugins/           # 插件
├── 08-Scripting/         # 脚本引擎
├── 09-Host/              # 启动宿主
├── 10-Tools/             # 辅助工具
├── 11-Samples/           # 示例
└── 12-Tests/             # 测试
```

### 实施策略

采用**渐进式实施**策略：

| 阶段 | 内容 | 优先级 |
|------|------|--------|
| MVP | Core + DI + Communication 基础 | P0 |
| Phase 1 | UI框架 + MVP + 基础控件 | P1 |
| Phase 2 | Repository + Services | P1 |
| Phase 3 | Scripting Engine | P2 |
| Phase 4 | HMI System | P2 |
| Phase 5 | Motion/Vision (预留) | P3 |

### 技术选型

| 组件 | 选择 | 理由 |
|------|------|------|
| 目标框架 | .NET 8.0 Windows | 长期支持 LTS |
| DI 容器 | Autofac | 功能丰富、社区活跃 |
| 日志 | Serilog | 灵活、可扩展 |
| ORM | SqlSugar | 轻量、高性能、适合工业场景 |
| 脚本引擎 | Roslyn | C# 原生支持 |
