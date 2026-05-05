# 工业级 WinForm 框架 - 实施任务清单

## MVP 阶段 (P0)

### Core Abstractions

- [ ] **T-001**: 创建解决方案 `Acme.Industrial.sln`
- [ ] **T-002**: 创建 `Directory.Build.props` (全局 MSBuild 属性)
- [ ] **T-003**: 创建 `Directory.Packages.props` (集中包版本管理)
- [ ] **T-004**: 创建 `.editorconfig` (代码风格)
- [ ] **T-005**: 实现 `OperateResult` / `OperateResult<T>`
- [ ] **T-006**: 实现 `ErrorCode` 错误码静态类
- [ ] **T-007**: 实现 `IServiceRegistry` / `IServiceResolver` 接口
- [ ] **T-008**: 实现 `AutofacServiceContainer` DI容器
- [ ] **T-009**: 实现 `IInitializable` / `IDisposableAsync` 接口
- [ ] **T-010**: 实现 `IModule` 接口和 `ModuleMetadata`
- [ ] **T-011**: 实现 `IModuleRegistry` 模块注册中心
- [ ] **T-012**: 实现 `IModuleLoadStrategy` 加载策略 (Eager/Lazy/Preload/Manual)
- [ ] **T-013**: 实现 `IModuleManager` 模块生命周期管理
- [ ] **T-014**: 实现 `ApplicationBootstrap` 启动引导器
- [ ] **T-015**: 实现 `IPlugin` 插件接口
- [ ] **T-016**: 实现 `IEventBus` / `EventBase` 事件总线
- [ ] **T-017**: 实现 `IAppLogger` / `IAppLoggerFactory` 日志接口
- [ ] **T-018**: 实现 `IAppConfiguration` 配置接口
- [ ] **T-019**: 实现 `IAppCache` 缓存接口

### Communication Layer

- [x] **T-020**: 创建 `IDeviceDriver` 接口
- [x] **T-021**: 实现 `DeviceDriverBase` 抽象基类
- [x] **T-022**: 实现 `ITagSubscriber` 订阅接口
- [x] **T-023**: 实现 `IDeviceManager` 设备管理器
- [x] **T-024**: 实现 `IDriverFactory` 驱动工厂
- [x] **T-025**: 实现 `IByteTransform` 字节序接口
- [x] **T-026**: 实现 `MockDeviceDriver` 模拟驱动
- [x] **T-027**: 实现 `ModbusTcpDriver` Modbus TCP 驱动
- [x] **T-028**: 实现 `SiemensS7Driver` 西门子 S7 驱动
- [x] **T-029**: 实现 `MitsubishiMcDriver` 三菱 MC 驱动
- [x] **T-030**: 实现 `TagSubscriptionService` 点位订阅服务
- [x] **T-031**: 实现 `AddressGroup` 地址合并工具

---

## Phase 1 (P1)

### Data Access

- [ ] **T-032**: 实现 `IRepository<T, TKey>` 泛型仓储接口
- [ ] **T-033**: 实现 `RepositoryBase<T, TKey>` 仓储基类
- [ ] **T-034**: 实现 `IUnitOfWork` 工作单元
- [ ] **T-035**: 实现 `SqlSugarClientWrapper` SqlSugar 包装
- [ ] **T-036**: 创建基础实体模型 (User, Role, AuditLog)

### UI Framework

- [ ] **T-037**: 实现 `IView` / `IPresenter` MVP 接口
- [ ] **T-038**: 实现 `PresenterBase<TView>` Presenter 基类
- [ ] **T-039**: 实现 `BaseForm` 窗体基类
- [ ] **T-040**: 实现 `INavigationService` 导航服务
- [ ] **T-041**: 实现 `IMenuRegistry` 菜单注册
- [ ] **T-042**: 实现 `ILocalizer` 本地化接口
- [ ] **T-043**: 实现 `INotificationService` 通知服务
- [ ] **T-044**: 实现 `IndicatorLamp` 指示灯控件
- [ ] **T-045**: 实现 `DigitalDisplay` 数显控件
- [ ] **T-046**: 实现 `IndustrialButton` 工业按钮
- [ ] **T-047**: 实现 `RealtimeTrendChart` 实时趋势图

### Business Services

- [ ] **T-048**: 实现 `IDataAcquisitionService` 数据采集服务
- [ ] **T-049**: 实现 `IAlarmService` 报警服务
- [ ] **T-050**: 实现 `IHistorianService` 历史数据服务

---

## Phase 2 (P2)

### Scripting Engine

- [ ] **T-051**: 实现 `IScriptEngine` 脚本引擎接口
- [ ] **T-052**: 实现 `IScriptContext` 脚本上下文
- [ ] **T-053**: 实现 `IScriptManager` 脚本管理器
- [ ] **T-054**: 实现 `CSharpScriptEngine` Roslyn 实现
- [ ] **T-055**: 实现 `IScriptSandbox` 安全沙箱
- [ ] **T-056**: 实现 `IndustrialScriptGlobals` 内置全局对象
- [ ] **T-057**: 实现 `ScriptEditorControl` 脚本编辑器
- [ ] **T-058**: 实现脚本版本管理
- [ ] **T-059**: 实现脚本触发器 (定时/事件/点位变化)

### HMI System

- [ ] **T-060**: 定义 `HmiProject` / `HmiPage` 项目模型
- [ ] **T-061**: 定义 `HmiElement` 元素基类
- [ ] **T-062**: 实现 `IHmiDesigner` 设计器接口
- [ ] **T-063**: 实现 `IHmiRuntime` 运行时接口
- [ ] **T-064**: 实现 `HmiRuntimeEngine` 运行时引擎
- [ ] **T-065**: 实现基础图元 (Rectangle, Ellipse, Line, Text)
- [ ] **T-066**: 实现 `TagBinding` 数据绑定
- [ ] **T-067**: 实现 `HmiAnimation` 动画系统
- [ ] **T-068**: 实现设计器撤销/重做

---

## Phase 3 (P3 - 预留)

### Motion Control

- [ ] **T-069**: 实现 `IMotionController` 运动控制器接口
- [ ] **T-070**: 实现 `IMotionAxis` 单轴接口
- [ ] **T-071**: 实现 `IMotionAxisGroup` 轴组接口
- [ ] **T-072**: 实现 `MotionControllerBase` 控制器基类
- [ ] **T-073**: 实现 `BeckhoffTwinCATDriver` (预留)
- [ ] **T-074**: 实现 `MotionCoordinator` 运动协调器
- [ ] **T-075**: 实现 `SafetyMonitor` 安全监控

### Vision System

- [ ] **T-076**: 实现 `ICamera` 相机接口
- [ ] **T-077**: 实现 `IVisionTool` 视觉工具接口
- [ ] **T-078**: 实现 `IVisionSystem` 视觉系统接口
- [ ] **T-079**: 实现 `GigEVisionCamera` GigE 相机 (预留)
- [ ] **T-080**: 实现 `InspectionService` 检测服务
- [ ] **T-081**: 实现 `VisionHmiIntegration` HMI 集成

---

## 任务估算

| 阶段 | 任务数 | 预计人天 |
|------|--------|----------|
| MVP | 31 | 15 |
| Phase 1 | 19 | 10 |
| Phase 2 | 20 | 12 |
| Phase 3 | 13 | 8 |
| **总计** | **83** | **45** |

---

## 里程碑

| 里程碑 | 内容 | 验收标准 |
|--------|------|----------|
| M1 | MVP | Core + DI + DeviceDriver 基类 + Mock 驱动可运行 |
| M2 | Phase 1 | Repository + UI 基类 + 基础控件 + 数据采集服务 |
| M3 | Phase 2 | ScriptEngine + HMI 设计器 |
| M4 | Phase 3 | Motion + Vision 预留接口 |

---

## 依赖关系

```
M1 (MVP)
├── T-001 ~ T-019 (Core)
└── T-020 ~ T-031 (Communication)

M2 (Phase 1)
├── M1 完成
├── T-032 ~ T-036 (Data Access)
├── T-037 ~ T-047 (UI Framework)
└── T-048 ~ T-050 (Services)

M3 (Phase 2)
├── M2 完成
├── T-051 ~ T-059 (Scripting)
└── T-060 ~ T-068 (HMI)

M4 (Phase 3)
├── M3 完成
└── T-069 ~ T-081 (Motion/Vision)
```
