# 鸿蒙安装助手（HarmonyOS HAP Installer）

一个 Windows 上的鸿蒙（HarmonyOS NEXT）HAP 安装/签名工具，绿色免安装，单文件 + 3 个 WebView2 组件。

## 功能

- **华为账号登录**：内置 WebView2 登录窗口，不依赖第三方工具，自动获取华为开发者登录态
- **自动生成签名 Profile**：新包名自动检查/创建调试证书、注册设备、生成对应 Profile
- **一键签名安装**：签名 → 安装 → 启动，全自动
- **权限问题自动修复**：安装时遇到系统拒绝授予的权限，自动从安装包剔除后重签重装
- **设备管理**：USB/无线连接检测、卸载、启动
- **支持任意 HAP**：不限于某个应用

## 使用前提

1. Windows 10/11，已安装 **Edge WebView2 运行时**（Win11 自带）
2. **华为开发者账号**（用于登录和生成 Profile）
3. **无需准备任何签名材料**：工具已内置公共调试签名材料（见下文），登录华为账号后即可直接签名安装
4. 手机开启 USB/无线调试，解锁状态

## 内置签名材料说明

本工具在 `store/` 目录内置了一套**公共调试签名身份**，用于“登录即可安装”：

| 文件 | 说明 |
|---|---|
| `store/hmos.p12` | 调试私钥（keystore，别名 `hmos`，密码 `hmos123`） |
| `store/hmos-debug.cer` | 对应的调试证书 |
| `store/hmos.csr` | 证书请求文件（用于向华为云注册调试证书） |

**来源**：这套材料来自开源项目 **小白调试助手**（[likuai2010/auto-installer](https://github.com/likuai2010/auto-installer)）在其公开 Release 中附带的调试签名材料（原名 `xiaobai.p12` / `xiaobai-debug.cer` / `xiaobai.csr`）。该项目及其发布包均公开可下载，任何人均可获取，因此不属于任何个人的私密信息。本工具仅将其重命名（`hmos.*`）并内置，调整了 keystore 的别名（`xiaobai`→`hmos`）与密码（统一为 `hmos123`），**密钥本身未改动**。

**为什么可以这样用**：

- 鸿蒙调试证书（debug 证书）只用于开发调试与自装应用，不用于应用市场上架；
- 每次安装时，Profile 仍由**你自己的华为账号**生成（绑定你的包名和设备 UDID），密钥只是公共的“调试签名身份”；
- 因此新用户登录华为账号后即可直接签名安装，无需提前准备任何材料，体验与小白调试助手一致。

**隐私与安全提示**：

- 内置材料是公开的，任何拿到它的人都能用同一身份做调试签名，请勿把它当作私有密钥保管；
- 请勿将你自己的正式证书、私有 p12 或 Profile 上传到公开仓库；
- 如需完全独立的签名身份：删除 `store/` 下的内置文件，登录后让工具自动生成新的（或放入你自己的材料替换）；
- 正式发布（上架应用市场）请使用华为开发者平台签发的正式证书。

## 快速开始

1. 双击 `鸿蒙安装助手.exe`
2. 点“连接检测”确认手机在线
3. 点“华为账号登录”完成登录
4. 把 `.hap` 拖进窗口 → 点“① 签名并安装”

## 文件说明

- `鸿蒙安装助手.exe` —— 主程序（C# / .NET Framework，源码见 `src/`）
- `Microsoft.Web.WebView2.*.dll`、`WebView2Loader.dll` —— 内置登录窗口依赖
- `src/InstallerHelper.cs` —— 完整源码（用 csc 编译，见源码头部注释）

## 编译

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe ^
  /out:鸿蒙安装助手.exe ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ^
  /r:System.Web.Extensions.dll ^
  /r:Microsoft.Web.WebView2.Core.dll /r:Microsoft.Web.WebView2.WinForms.dll ^
  src\InstallerHelper.cs
```

> 需要 WebView2 NuGet 包（`Microsoft.Web.WebView2`）里的 `Microsoft.Web.WebView2.Core.dll` 和 `Microsoft.Web.WebView2.WinForms.dll`。

## 安全说明

- 内置的 `hmos.p12` 等材料来自小白调试助手的公开 Release，属于公共调试身份，非个人私密信息
- 你自己的签名材料（p12 / p7b / cer）属于你的签名身份，切勿上传或外传
- 本工具只在本机使用签名材料，不收集任何账号信息
- 鸿蒙正式机安装应用要求“包名 + Profile”一一对应，新包名需先用“生成Profile”创建

## 免责声明

本工具仅供个人学习和合法用途。请遵守华为开发者平台相关规则；因不当使用产生的后果由使用者自行承担。

## License

MIT（如需正式授权文本，请自行添加 LICENSE 文件）。
