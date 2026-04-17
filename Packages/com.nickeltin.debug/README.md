# Nickeltin Debug Panel
UIToolkit based debug panel, ideal for cheats in mobile games.


## Useful links
- [Samples documentation (START HERE)](https://nickeltin.notion.site/2c8cd34c79c280dd8df9e061bb3aa2bb)
- [Checkout my other asset - SDF Image](https://assetstore.unity.com/packages/tools/gui/sdf-image-quality-ui-outlines-and-shadow-244942)


## Folders structure
- Editor - editor assembly
- Runtime - runtime assembly
- Samples - samples, start exploring here
- CHANGELOG - changelog file
- package.json - package manifest required for valid npm package
- README - file you in, short version of info about package

## Get started
- On scene RMB => UI => DebugPanel to create panel
- Create script that inhertis from `nickeltin.DebugPanel.Runtime.ContentProvider.cs`
- Override the `PopuplateContent(DebugPanel panel)` method with creation of content with
    - `panel.BeingGroup()` / `panle.EndGroup()`
    - `panel.AddButton()`
    - `panel.AddToggle()`
    - etc
- Attach your `CustomContentProvider.cs` to created DebugPanel
- Voila, debug panel now displays your custom UI structure

## Samples
- Go to PackageManager and find SDFImage package
- Expand Samples section and click Import
- There you go, samples is now imported to Assets folder, go ahead and explore



