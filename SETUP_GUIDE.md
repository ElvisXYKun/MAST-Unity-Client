# M.A.S.T. Unity Client — 场景配置步骤指南

本文件指导你在 Unity Editor 中完成场景搭建与脚本连接。
所有 C# 脚本已预先生成在 `Assets/Scripts/` 目录下，无需手动输入代码。

---

## 步骤一：打开 Unity 并加载工程

1. 打开 Unity Hub → **Open → Add project from disk**
2. 选择本目录（`MAST-Unity-Client/`），点击 Open
3. Unity 加载工程后，在 **Project 窗口 → Assets** 可以看到 Scripts 文件夹

---

## 步骤二：安装依赖插件（只需一次）

### 安装 NativeWebSocket
1. 菜单 **Window → Package Manager**
2. 点击左上角 `+` → **Add package from git URL...**
3. 粘贴：`https://github.com/endel/NativeWebSocket.git#upm` → 点击 Add

### 安装 Newtonsoft.Json
1. 同上，在 Package Manager 中 `+` → **Add package from git URL...**
2. 粘贴：`com.unity.nuget.newtonsoft-json` → 点击 Add

---

## 步骤三：创建场景对象

在 **Hierarchy 窗口**，右键空白区域，依次创建：

| 操作 | 命名 | 备注 |
|---|---|---|
| 右键 → 3D Object → Sphere | `TrackingTarget` | 追踪目标球体，Scale 改为 (0.1, 0.1, 0.1) |
| 右键 → Create Empty | `MastManager` | 挂载所有脚本的空对象 |
| 右键 → UI → Canvas | `HUD` | UI 根节点 |
| 在 HUD 下 → UI → Text - TextMeshPro | `StatusText` | 连接状态文字 |
| 在 HUD 下 → UI → Text - TextMeshPro | `CoordText` | 坐标数值显示 |
| 在 HUD 下 → UI → Input Field - TextMeshPro | `IPInputField` | IP 地址输入框 |

---

## 步骤四：挂载脚本到 MastManager

在 Hierarchy 中**点击选中 MastManager**，然后在 Inspector 中：

1. 点击 **Add Component** → 搜索并添加 `MastClient`
2. 点击 **Add Component** → 搜索并添加 `TrackingVisualizer`
3. 点击 **Add Component** → 搜索并添加 `UnityMainThreadDispatcher`
4. 点击 **Add Component** → 搜索并添加 `UIPanel`

---

## 步骤五：在 Inspector 中配置引用关系

选中 `MastManager`，在 Inspector 中找到以下组件，完成拖拽连接：

### MastClient 组件
| 字段 | 填写内容 |
|---|---|
| `Server IP` | 运行 demo_server.py 的 Mac 局域网 IP（如 192.168.1.5） |
| `On Position Received → +` | 拖入 MastManager → 函数选 TrackingVisualizer.OnNewPosition |
| `On Position Received → +` | 再加一个 → 拖入 MastManager → 函数选 UIPanel.UpdatePosition |
| `On Status Changed → +` | 拖入 MastManager → 函数选 UIPanel.UpdateStatus |

### TrackingVisualizer 组件
| 字段 | 拖入对象 |
|---|---|
| `Tracking Target` | 从 Hierarchy 拖入 `TrackingTarget` 球体 |

### UIPanel 组件
| 字段 | 拖入对象 |
|---|---|
| `Status Text` | 从 Hierarchy 拖入 `StatusText` |
| `Ip Input Field` | 从 Hierarchy 拖入 `IPInputField` |
| `Coordinate Text` | 从 Hierarchy 拖入 `CoordText` |

---

## 步骤六：本地测试

1. 在 Mac 终端启动服务端：
   ```bash
   cd /path/to/M.A.S.T
   python examples/demo_server.py
   ```
2. 在 Unity 中点击顶部 **▶ Play** 按钮
3. 在 Game 窗口中的 IP 输入框填写 `127.0.0.1`（本地测试）
4. StatusText 变为 `🟢 已连接` 后，追踪球体开始沿 8 字形运动

---

## 步骤七：打包到 iPad

1. **File → Build Settings → iOS → Switch Platform**
2. **Player Settings** 中填写：
   - Bundle Identifier: `com.yourname.masttracker`
   - Target iOS Version: `16.0`
   - Other Settings → Allow HTTP: `Always allowed`
3. 连接 iPad → **Build And Run** → 选择桌面新建的 Build 文件夹
4. Xcode 自动打开 → 选择 iPad 设备 → ▶ Run
5. iPad 上打开 App，在 IP 输入框填写 Mac 的局域网 IP

---

## 查找 Mac 局域网 IP

**系统设置 → Wi-Fi → 已连接网络右侧 ⓘ → IP 地址**

例如：`192.168.1.5`，在 iPad App 中填入 `192.168.1.5` 即可。

> ⚠️ iPad 和 Mac 必须连接**同一个 Wi-Fi 网络**！
