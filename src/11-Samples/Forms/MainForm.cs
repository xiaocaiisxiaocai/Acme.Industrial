using System;
using System.Drawing;
using System.Windows.Forms;
using IComp = Acme.Industrial.UI.Controls.Industrial;

namespace Acme.Industrial.Samples.Forms;

/// <summary>
/// 主演示窗口。
/// </summary>
public class MainForm : Form
{
    private readonly TabControl _tabControl;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;

    public MainForm()
    {
        Text = "工业控件库演示 - Industrial Controls Demo";
        Size = new Size(1400, 950);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 35);
        ForeColor = Color.White;

        // 菜单栏
        var menuStrip = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("文件");
        fileMenu.DropDownItems.Add("退出", null, (s, e) => Close());
        menuStrip.Items.Add(fileMenu);
        var demoMenu = new ToolStripMenuItem("演示");
        demoMenu.DropDownItems.Add("指示类控件", null, (s, e) => SelectTab(0));
        demoMenu.DropDownItems.Add("容器类控件", null, (s, e) => SelectTab(1));
        demoMenu.DropDownItems.Add("图表类控件", null, (s, e) => SelectTab(2));
        demoMenu.DropDownItems.Add("设备类控件", null, (s, e) => SelectTab(3));
        demoMenu.DropDownItems.Add("交互类控件", null, (s, e) => SelectTab(4));
        menuStrip.Items.Add(demoMenu);
        Controls.Add(menuStrip);

        // 状态栏
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("就绪");
        _statusLabel.ForeColor = Color.LightGray;
        _statusStrip.Items.Add(_statusLabel);
        Controls.Add(_statusStrip);

        // 选项卡
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Location = new Point(0, menuStrip.Height),
            Size = new Size(Width, Height - menuStrip.Height - _statusStrip.Height),
            SelectedIndex = 0,
            BackColor = Color.FromArgb(35, 35, 40)
        };

        // 创建演示页面
        _tabControl.TabPages.Add(CreateIndicatorTab());
        _tabControl.TabPages.Add(CreateContainerTab());
        _tabControl.TabPages.Add(CreateChartTab());
        _tabControl.TabPages.Add(CreateDeviceTab());
        _tabControl.TabPages.Add(CreateInteractiveTab());

        _tabControl.SelectedIndexChanged += (s, e) =>
        {
            _statusLabel.Text = $"当前页面: {_tabControl.SelectedTab?.Text}";
        };

        Controls.Add(_tabControl);
    }

    private void SelectTab(int index)
    {
        if (index < _tabControl.TabPages.Count)
            _tabControl.SelectedIndex = index;
    }

    private TabPage CreateIndicatorTab()
    {
        var page = new TabPage("指示类控件");
        page.BackColor = Color.FromArgb(35, 35, 40);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(15)
        };

        // 模拟仪表
        var gaugePanel = CreateControlPanel("模拟仪表", "指针/扇形/填充样式", 220, 260);
        var gauge = new IComp.AnalogGauge { Value = 65, WarningValue = 70, DangerValue = 90, Size = new Size(200, 200) };
        gaugePanel.Controls.Add(gauge);
        flow.Controls.Add(gaugePanel);

        // 温度计
        var tempPanel = CreateControlPanel("温度计", "垂直/水平显示", 120, 260);
        var thermometer = new IComp.ThermometerControl { Value = 45, Size = new Size(80, 200) };
        tempPanel.Controls.Add(thermometer);
        flow.Controls.Add(tempPanel);

        // 指示灯
        var lampPanel = CreateControlPanel("指示灯", "多种颜色和状态", 100, 200);
        var lampFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        lampFlow.Controls.Add(new Label { Text = "运行", ForeColor = Color.LightGray });
        lampFlow.Controls.Add(new IComp.IndicatorLamp { ColorScheme = IComp.LampColorScheme.Green, Size = new Size(40, 40), Margin = new Padding(5) });
        lampFlow.Controls.Add(new Label { Text = "警告", ForeColor = Color.LightGray });
        lampFlow.Controls.Add(new IComp.IndicatorLamp { ColorScheme = IComp.LampColorScheme.Yellow, Size = new Size(40, 40), Margin = new Padding(5) });
        lampFlow.Controls.Add(new Label { Text = "故障", ForeColor = Color.LightGray });
        lampFlow.Controls.Add(new IComp.IndicatorLamp { ColorScheme = IComp.LampColorScheme.Red, Size = new Size(40, 40), Margin = new Padding(5) });
        lampFlow.Controls.Add(new Label { Text = "待机", ForeColor = Color.LightGray });
        lampFlow.Controls.Add(new IComp.IndicatorLamp { ColorScheme = IComp.LampColorScheme.Blue, Size = new Size(40, 40), Margin = new Padding(5) });
        lampPanel.Controls.Add(lampFlow);
        flow.Controls.Add(lampPanel);

        // 数码管
        var displayPanel = CreateControlPanel("数码管", "七段显示", 180, 100);
        var digitalDisplay = new IComp.DigitalDisplay { Value = "123.45", DigitCount = 6, DecimalPlaces = 2, Size = new Size(160, 60) };
        displayPanel.Controls.Add(digitalDisplay);
        flow.Controls.Add(displayPanel);

        // 开关按钮
        var switchPanel = CreateControlPanel("开关按钮", "多种样式", 200, 200);
        var switchFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        switchFlow.Controls.Add(new Label { Text = "标准样式", ForeColor = Color.LightGray });
        switchFlow.Controls.Add(new IComp.ToggleSwitch { IsOn = true, Style = IComp.ToggleStyle.Standard, Size = new Size(80, 35), Margin = new Padding(5) });
        switchFlow.Controls.Add(new Label { Text = "药丸样式", ForeColor = Color.LightGray });
        switchFlow.Controls.Add(new IComp.ToggleSwitch { IsOn = false, Style = IComp.ToggleStyle.Pill, Size = new Size(80, 35), Margin = new Padding(5) });
        switchFlow.Controls.Add(new Label { Text = "工业样式", ForeColor = Color.LightGray });
        switchFlow.Controls.Add(new IComp.ToggleSwitch { IsOn = true, Style = IComp.ToggleStyle.Industrial, Size = new Size(80, 35), Margin = new Padding(5) });
        switchPanel.Controls.Add(switchFlow);
        flow.Controls.Add(switchPanel);

        // 工业按钮
        var btnPanel = CreateControlPanel("工业按钮", "多种状态", 200, 200);
        var btnFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        btnFlow.Controls.Add(new IComp.IndustrialButton { ButtonStyle = IComp.IndustrialButtonStyle.Standard, Text = "标准按钮", Size = new Size(120, 40), Margin = new Padding(5) });
        btnFlow.Controls.Add(new IComp.IndustrialButton { ButtonStyle = IComp.IndustrialButtonStyle.Circular, Text = "○", Size = new Size(60, 60), Margin = new Padding(5) });
        btnFlow.Controls.Add(new IComp.IndustrialButton { ButtonStyle = IComp.IndustrialButtonStyle.Emergency, Text = "急停", Size = new Size(80, 80), Margin = new Padding(5) });
        btnPanel.Controls.Add(btnFlow);
        flow.Controls.Add(btnPanel);

        // 液位计
        var levelPanel = CreateControlPanel("液位计", "波浪动画", 120, 260);
        var level = new IComp.LevelIndicator { Value = 65, Size = new Size(80, 200) };
        levelPanel.Controls.Add(level);
        flow.Controls.Add(levelPanel);

        // 棒图
        var barPanel = CreateControlPanel("棒图", "水平/垂直", 280, 180);
        var barChart = new IComp.BarChart { IsHorizontal = false, Size = new Size(250, 140) };
        barChart.Values = new System.Collections.Generic.List<double> { 30, 50, 70, 40, 90 };
        barChart.Labels = new System.Collections.Generic.List<string> { "A区", "B区", "C区", "D区", "E区" };
        barPanel.Controls.Add(barChart);
        flow.Controls.Add(barPanel);

        page.Controls.Add(flow);
        return page;
    }

    private TabPage CreateContainerTab()
    {
        var page = new TabPage("容器类控件");
        page.BackColor = Color.FromArgb(35, 35, 40);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(15)
        };

        // 料仓
        var tankPanel = CreateControlPanel("料仓", "带支腿和温度", 140, 260);
        var tank = new IComp.TankControl { Level = 72, TankName = "原料仓", ShowTemperature = true, Temperature = 35.5, Size = new Size(120, 220) };
        tankPanel.Controls.Add(tank);
        flow.Controls.Add(tankPanel);

        // 液位计
        var levelPanel = CreateControlPanel("液位计", "支持气泡", 120, 260);
        var level = new IComp.LevelIndicator { Value = 55, ShowBubbles = true, BubbleCount = 8, Size = new Size(80, 200) };
        levelPanel.Controls.Add(level);
        flow.Controls.Add(levelPanel);

        // 多个料仓
        var tanksPanel = CreateControlPanel("料仓组", "多容器", 400, 260);
        var tanksFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10) };
        tanksFlow.Controls.Add(new IComp.TankControl { Level = 80, TankName = "储罐1", Size = new Size(100, 200) });
        tanksFlow.Controls.Add(new IComp.TankControl { Level = 45, TankName = "储罐2", Size = new Size(100, 200) });
        tanksFlow.Controls.Add(new IComp.TankControl { Level = 90, TankName = "储罐3", Size = new Size(100, 200) });
        tanksPanel.Controls.Add(tanksFlow);
        flow.Controls.Add(tanksPanel);

        // 水平料仓
        var hTankPanel = CreateControlPanel("水平料仓", "水平布局", 350, 160);
        var hTank = new IComp.TankControl { Level = 60, IsHorizontal = true, Size = new Size(300, 100), TankName = "水平罐" };
        hTankPanel.Controls.Add(hTank);
        flow.Controls.Add(hTankPanel);

        // 动态液位计
        var bubblePanel = CreateControlPanel("动态液位计", "波浪+气泡", 120, 260);
        var bubbleLevel = new IComp.LevelIndicator { Value = 40, ShowBubbles = true, WaveAmplitude = 5, Size = new Size(80, 200) };
        bubblePanel.Controls.Add(bubbleLevel);
        flow.Controls.Add(bubblePanel);

        // 管道+料仓
        var pipeTankPanel = CreateControlPanel("料仓系统", "管道+料仓", 250, 300);
        var pipeTankFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10) };
        var pipe = new IComp.PipeControl { Direction = IComp.PipeDirection.Top | IComp.PipeDirection.Bottom, ShowValve = true, ValveOpen = true, ShowPump = true, PumpRunning = true, Size = new Size(60, 250) };
        var smallTank = new IComp.TankControl { Level = 75, Size = new Size(100, 250), TankName = "混合罐" };
        pipeTankFlow.Controls.Add(pipe);
        pipeTankFlow.Controls.Add(smallTank);
        pipeTankPanel.Controls.Add(pipeTankFlow);
        flow.Controls.Add(pipeTankPanel);

        page.Controls.Add(flow);
        return page;
    }

    private TabPage CreateChartTab()
    {
        var page = new TabPage("图表类控件");
        page.BackColor = Color.FromArgb(35, 35, 40);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(15)
        };

        // 实时趋势图
        var trendPanel = CreateControlPanel("实时趋势图", "多曲线显示", 450, 280);
        var trendChart = new IComp.RealtimeTrendChart { ShowLegend = true, Size = new Size(420, 240) };
        trendChart.AddCurve("温度", Color.Red);
        trendChart.AddCurve("压力", Color.Blue);
        trendChart.AddCurve("流量", Color.Green);
        trendPanel.Controls.Add(trendChart);
        flow.Controls.Add(trendPanel);

        // 饼图
        var piePanel = CreateControlPanel("饼图", "比例展示", 320, 300);
        var pieChart = new IComp.PieChart { ChartType = IComp.PieChartType.Pie, ShowLegend = true, ShowPercentages = true, Size = new Size(290, 260) };
        pieChart.Items = new System.Collections.Generic.List<IComp.PieChartItem>
        {
            new() { Label = "A产品", Value = 35, Color = Color.FromArgb(0, 150, 200) },
            new() { Label = "B产品", Value = 25, Color = Color.FromArgb(50, 180, 50) },
            new() { Label = "C产品", Value = 20, Color = Color.FromArgb(255, 180, 0) },
            new() { Label = "D产品", Value = 20, Color = Color.FromArgb(220, 80, 80) }
        };
        piePanel.Controls.Add(pieChart);
        flow.Controls.Add(piePanel);

        // 水平棒图
        var barPanel = CreateControlPanel("水平棒图", "数据对比", 400, 200);
        var barChart = new IComp.BarChart { IsHorizontal = true, ShowValues = true, Size = new Size(370, 160) };
        barChart.Values = new System.Collections.Generic.List<double> { 85, 62, 78, 45, 92, 55 };
        barChart.Labels = new System.Collections.Generic.List<string> { "车间1", "车间2", "车间3", "车间4", "车间5", "车间6" };
        barPanel.Controls.Add(barChart);
        flow.Controls.Add(barPanel);

        // 环形图
        var donutPanel = CreateControlPanel("环形图", "3D效果", 320, 300);
        var donutChart = new IComp.PieChart { ChartType = IComp.PieChartType.Donut, DonutRadius = 50, Draw3D = true, ShowLegend = true, Size = new Size(290, 260) };
        donutChart.Items = new System.Collections.Generic.List<IComp.PieChartItem>
        {
            new() { Label = "已完成", Value = 65, Color = Color.FromArgb(50, 180, 50) },
            new() { Label = "进行中", Value = 25, Color = Color.FromArgb(0, 150, 200) },
            new() { Label = "待处理", Value = 10, Color = Color.FromArgb(255, 180, 0) }
        };
        donutPanel.Controls.Add(donutChart);
        flow.Controls.Add(donutPanel);

        page.Controls.Add(flow);
        return page;
    }

    private TabPage CreateDeviceTab()
    {
        var page = new TabPage("设备类控件");
        page.BackColor = Color.FromArgb(35, 35, 40);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(15)
        };

        // 球阀
        var valve1Panel = CreateControlPanel("球阀", "开关控制", 140, 180);
        valve1Panel.Controls.Add(new IComp.ValveControl { ValveType = IComp.ValveType.Ball, State = IComp.ValveState.Open, LabelText = "进水阀", Size = new Size(120, 140) });
        flow.Controls.Add(valve1Panel);

        // 蝶阀
        var butterflyPanel = CreateControlPanel("蝶阀", "旋转手柄", 140, 180);
        butterflyPanel.Controls.Add(new IComp.ValveControl { ValveType = IComp.ValveType.Butterfly, State = IComp.ValveState.Closed, OpenPosition = 0, LabelText = "排气阀", Size = new Size(120, 140) });
        flow.Controls.Add(butterflyPanel);

        // 闸阀
        var gatePanel = CreateControlPanel("闸阀", "升降闸板", 140, 180);
        gatePanel.Controls.Add(new IComp.ValveControl { ValveType = IComp.ValveType.Gate, OpenPosition = 50, LabelText = "主闸阀", Size = new Size(120, 140) });
        flow.Controls.Add(gatePanel);

        // 截止阀
        var globePanel = CreateControlPanel("截止阀", "精确调节", 140, 180);
        globePanel.Controls.Add(new IComp.ValveControl { ValveType = IComp.ValveType.Globe, OpenPosition = 75, LabelText = "调节阀", Size = new Size(120, 140) });
        flow.Controls.Add(globePanel);

        // 电机
        var motorPanel = CreateControlPanel("电机", "运行状态", 250, 300);
        var motorFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        motorFlow.Controls.Add(new IComp.MotorControl { State = IComp.MotorState.Running, Direction = IComp.MotorDirection.Forward, Speed = 85, LabelText = "主电机", Size = new Size(100, 130) });
        motorFlow.Controls.Add(new IComp.MotorControl { State = IComp.MotorState.Stopped, LabelText = "备用电机", Size = new Size(100, 130) });
        motorPanel.Controls.Add(motorFlow);
        flow.Controls.Add(motorPanel);

        // 管道样式
        var pipePanel = CreateControlPanel("管道", "多种样式", 500, 200);
        var pipeFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        pipeFlow.Controls.Add(new IComp.PipeControl { Direction = IComp.PipeDirection.Left | IComp.PipeDirection.Right, PipeStyle = IComp.PipeStyle.Solid, Size = new Size(200, 30) });
        pipeFlow.Controls.Add(new IComp.PipeControl { Direction = IComp.PipeDirection.Top | IComp.PipeDirection.Bottom, PipeStyle = IComp.PipeStyle.Flow, ShowFlowArrow = true, Size = new Size(50, 100) });
        pipeFlow.Controls.Add(new IComp.PipeControl { Direction = IComp.PipeDirection.Left | IComp.PipeDirection.Right, PipeStyle = IComp.PipeStyle.Dashed, Size = new Size(200, 20) });
        pipePanel.Controls.Add(pipeFlow);
        flow.Controls.Add(pipePanel);

        // 泵站
        var pumpPanel = CreateControlPanel("泵站", "泵+阀门", 200, 300);
        var pumpFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10) };
        pumpFlow.Controls.Add(new IComp.PipeControl { Direction = IComp.PipeDirection.Top | IComp.PipeDirection.Bottom, ShowPump = true, PumpRunning = true, Size = new Size(50, 250) });
        pumpFlow.Controls.Add(new IComp.ValveControl { ValveType = IComp.ValveType.Ball, State = IComp.ValveState.Open, Size = new Size(80, 140) });
        pumpPanel.Controls.Add(pumpFlow);
        flow.Controls.Add(pumpPanel);

        page.Controls.Add(flow);
        return page;
    }

    private TabPage CreateInteractiveTab()
    {
        var page = new TabPage("交互类控件");
        page.BackColor = Color.FromArgb(35, 35, 40);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(15)
        };

        // 数字键盘
        var keypadPanel = CreateControlPanel("数字键盘", "输入数值", 280, 400);
        var keypad = new IComp.NumericKeypad { Mode = IComp.KeypadMode.Bounded, Minimum = 0, Maximum = 100, DecimalPlaces = 2, Size = new Size(250, 360) };
        keypad.Confirmed += (s, e) => _statusLabel.Text = $"输入值: {keypad.Value}";
        keypadPanel.Controls.Add(keypad);
        flow.Controls.Add(keypadPanel);

        // 报警列表
        var alarmPanel = CreateControlPanel("报警列表", "实时报警", 450, 400);
        var alarmList = new IComp.AlarmListControl { Size = new Size(420, 360) };
        alarmList.AddAlarm("温度传感器1", "温度超过阈值80°C", IComp.AlarmLevel.Warning, "85.2°C", "80°C");
        alarmList.AddAlarm("压力传感器", "压力异常", IComp.AlarmLevel.Error, "1.8MPa", "1.5MPa");
        alarmList.AddAlarm("液位计", "液位过低", IComp.AlarmLevel.Critical, "5%", "10%");
        alarmList.AddAlarm("流量计", "流量波动", IComp.AlarmLevel.Info, "正常范围", "-");
        alarmPanel.Controls.Add(alarmList);
        flow.Controls.Add(alarmPanel);

        // 操作按钮
        var btnPanel = CreateControlPanel("操作按钮", "交互操作", 200, 300);
        var btnFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        var startBtn = new IComp.IndustrialButton { ButtonStyle = IComp.IndustrialButtonStyle.Standard, Text = "启动", Size = new Size(120, 45) };
        startBtn.Click += (s, e) => _statusLabel.Text = "系统启动中...";
        var stopBtn = new IComp.IndustrialButton { ButtonStyle = IComp.IndustrialButtonStyle.Standard, Text = "停止", Size = new Size(120, 45) };
        stopBtn.Click += (s, e) => _statusLabel.Text = "系统停止中...";
        var resetBtn = new IComp.IndustrialButton { ButtonStyle = IComp.IndustrialButtonStyle.Circular, Text = "复位", Size = new Size(60, 60) };
        resetBtn.Click += (s, e) => _statusLabel.Text = "系统复位完成";
        var emergencyBtn = new IComp.IndustrialButton { ButtonStyle = IComp.IndustrialButtonStyle.Emergency, Text = "急停", Size = new Size(80, 80) };
        emergencyBtn.Click += (s, e) => _statusLabel.Text = "紧急停止!";
        btnFlow.Controls.Add(startBtn);
        btnFlow.Controls.Add(stopBtn);
        btnFlow.Controls.Add(resetBtn);
        btnFlow.Controls.Add(emergencyBtn);
        btnPanel.Controls.Add(btnFlow);
        flow.Controls.Add(btnPanel);

        // 开关控制面板
        var switchPanel = CreateControlPanel("开关控制", "批量开关", 200, 280);
        var switchGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(10) };
        var toggle1 = new IComp.ToggleSwitch { IsOn = true, Style = IComp.ToggleStyle.Pill, Size = new Size(70, 30) };
        var toggle2 = new IComp.ToggleSwitch { IsOn = false, Style = IComp.ToggleStyle.Pill, Size = new Size(70, 30) };
        var toggle3 = new IComp.ToggleSwitch { IsOn = true, Style = IComp.ToggleStyle.Pill, Size = new Size(70, 30) };
        var toggle4 = new IComp.ToggleSwitch { IsOn = true, Style = IComp.ToggleStyle.Pill, Size = new Size(70, 30) };
        toggle1.CheckedChanged += (s, e) => _statusLabel.Text = $"加热器: {(toggle1.IsOn ? "开启" : "关闭")}";
        toggle2.CheckedChanged += (s, e) => _statusLabel.Text = $"冷却泵: {(toggle2.IsOn ? "开启" : "关闭")}";
        toggle3.CheckedChanged += (s, e) => _statusLabel.Text = $"搅拌器: {(toggle3.IsOn ? "开启" : "关闭")}";
        toggle4.CheckedChanged += (s, e) => _statusLabel.Text = $"阀门: {(toggle4.IsOn ? "开启" : "关闭")}";
        switchGrid.Controls.Add(new Label { Text = "加热器:", ForeColor = Color.LightGray }, 0, 0);
        switchGrid.Controls.Add(toggle1, 1, 0);
        switchGrid.Controls.Add(new Label { Text = "冷却泵:", ForeColor = Color.LightGray }, 0, 1);
        switchGrid.Controls.Add(toggle2, 1, 1);
        switchGrid.Controls.Add(new Label { Text = "搅拌器:", ForeColor = Color.LightGray }, 0, 2);
        switchGrid.Controls.Add(toggle3, 1, 2);
        switchGrid.Controls.Add(new Label { Text = "阀门:", ForeColor = Color.LightGray }, 0, 3);
        switchGrid.Controls.Add(toggle4, 1, 3);
        switchPanel.Controls.Add(switchGrid);
        flow.Controls.Add(switchPanel);

        page.Controls.Add(flow);
        return page;
    }

    private Panel CreateControlPanel(string title, string description, int width, int height)
    {
        var panel = new Panel
        {
            Size = new Size(width, height),
            BackColor = Color.FromArgb(45, 45, 50),
            Margin = new Padding(10)
        };

        var titleLabel = new Label
        {
            Text = title,
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        var descLabel = new Label
        {
            Text = description,
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("微软雅黑", 8),
            Dock = DockStyle.Top,
            Height = 18,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        panel.Controls.Add(descLabel);
        panel.Controls.Add(titleLabel);

        return panel;
    }
}
