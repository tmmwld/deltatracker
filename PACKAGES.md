# System.Drawing.Common Package Reference

The application uses `System.Drawing.Common` for bitmap operations and screen capture.

Add this package reference if needed:

```xml
<PackageReference Include="System.Drawing.Common" Version="7.0.0" />
```

Note: System.Drawing.Common is included in .NET 6.0 for Windows applications by default.
However, if you encounter issues, you may need to explicitly reference it.

## Windows Forms Reference

For screen bounds and hotkey support, the project also uses Windows Forms:

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" />
</ItemGroup>
```

This is automatically included when using `TargetFramework` of `net6.0-windows` with `UseWPF` set to true.
