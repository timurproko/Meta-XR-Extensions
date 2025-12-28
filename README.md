# Meta XR Extensions

This package provides extensions for Meta XR SDK, including the ability to create custom Building Blocks that appear in the Meta XR SDK Building Blocks panel.

## Features

- **Custom Building Blocks**: Create custom Building Blocks that integrate seamlessly with Meta XR SDK
- **Easy Creation**: Create Building Block assets with a simple context menu
- **Automatic Integration**: Custom blocks automatically appear in the Building Blocks window alongside Meta's official blocks
- **Management Window**: View and manage all your custom Building Blocks in one place
- **Automatic Tag Validation**: Invalid or restricted tags are automatically cleaned from blocks
- **Automatic Injection**: Custom blocks are automatically injected into the Building Blocks registry
- **Custom Inspector**: Enhanced inspector for editing Building Block properties
- **UIToolkit Integration**: Additional UIToolkit-related Building Blocks and scripts (see UIToolkit section)

## Building Blocks Usage

### Creating a Building Block

1. **Context Menu Method**:
   - In the Project window, right-click where you want to create the block
   - Select `Create > Meta > Create Building Block`
   - A new `CustomBlockData` asset will be created
   - Configure the block by assigning a prefab, setting the name, description, thumbnail, and other properties

### Managing Building Blocks

Open the management window:
- Go to `Meta > Building Blocks > Manage Building Blocks`

In the management window you can:
- View all custom Building Blocks with thumbnails
- Click on a block to select it and view its details in the Inspector
- See block IDs, names, and descriptions

### Refreshing Building Blocks

If your custom blocks don't appear in the Building Blocks window:
- Go to `Meta > Building Blocks > Refresh Building Blocks`
- This will force a refresh of the Building Blocks registry

### Using Custom Building Blocks

Once created and configured, your custom Building Blocks will automatically appear in the Meta XR SDK Building Blocks window:
- Open the Building Blocks window (usually accessible from the Meta XR SDK status menu)
- Your custom blocks will be displayed alongside Meta's official blocks
- Click on a block to install it into your scene

## How It Works

1. **BlockData Assets**: Each Building Block is represented by a `CustomBlockData` ScriptableObject asset. When created via the context menu, the asset is created in the currently selected folder. When created programmatically via `CustomBlockDataCreator`, blocks are stored in `Assets/Tools/BuildingBlocks/BlockData/` by default (created automatically if needed).

2. **Prefab Setup**: When using the `CustomBlockDataCreator` API (available in code), the system automatically:
   - Adds a `BuildingBlock` component to your prefab
   - Sets the `blockId` to link the prefab to its BlockData
   - Handles prefab naming (removes `[BuildingBlock]` prefix if present)

3. **Automatic Discovery**: The `CustomBlockDataInjector` system automatically discovers all `CustomBlockData` assets in your project and injects them into the Meta XR SDK Building Blocks registry.

4. **Tag Validation**: The `TagValidationService` validates tags against Meta SDK's valid tag names. Invalid tags (including "Internal", "Hidden", and unrecognized tags) are automatically removed from blocks.

## File Structure

```
Tools/
  Meta XR Extensions/
    Core/
      Editor/
        Scripts/
          CustomBlockData.cs                    # Custom BlockData class inheriting from Meta's BlockData
          CustomBlockDataAssetPostprocessor.cs  # Automatic tag validation on asset import
          CustomBlockDataCreator.cs             # Utility API for creating blocks from prefabs
          CustomBlockDataEditor.cs              # Custom inspector with enhanced editing
          CustomBlockDataInjector.cs            # Automatic injection into Building Blocks registry
          CustomBlockDataMenu.cs                # Context menu items
          CustomBlockDataWindow.cs              # Management window
          TagValidationService.cs               # Validates tags against Meta SDK
        BuildingBlocks.asmdef                   # Assembly definition
    UIToolkit/
      (UIToolkit Building Block and scripts)
    Samples/
      (Sample scenes and examples)
    package.json
    README.md
```

## Technical Details

### Core Components

- **CustomBlockData**: Inherits from `Meta.XR.BuildingBlocks.Editor.BlockData` to integrate with the Meta XR SDK Building Blocks system. Includes automatic prefab name synchronization via `OnValidate`.

- **CustomBlockDataEditor**: Custom inspector that provides:
  - Enhanced editing of all block properties
  - Automatic tag validation and cleaning
  - Automatic asset and prefab renaming when block name changes
  - Installation status display
  - Support for all BlockData properties (dependencies, documentation URLs, etc.)

- **CustomBlockDataInjector**: Initializes on editor load and:
  - Automatically injects custom blocks into the Building Blocks registry
  - Listens for project changes and registry refresh events
  - Provides manual refresh functionality via menu item

- **CustomBlockDataAssetPostprocessor**: Automatically validates and cleans tags when BlockData assets are imported or modified.

- **TagValidationService**: Uses reflection to discover valid tag names from Meta SDK and provides validation services. Caches results for performance.

- **CustomBlockDataCreator**: Provides API methods (`CreateBlockDataFromPrefab`, `CreateBlockDataFromPrefabs`) for programmatically creating blocks from prefabs. This is used internally but can also be called from custom scripts.

- **CustomBlockDataWindow**: Management window showing all custom blocks with thumbnails, IDs, names, and descriptions. Click blocks to select them.

### Reflection Usage

The system uses reflection to:
- Set internal fields in `BlockData` and `BuildingBlock` components (id, version, blockName, description, etc.)
- Access the Building Blocks registry and filtered registry
- Discover valid tag names from Meta SDK's internal tag system
- Mark the filtered registry as dirty to trigger refreshes

## UIToolkit

This package includes UIToolkit integration with:
- UIToolkit Building Block (prefab and asset)
- Various UIToolkit interaction scripts for XR input
- Sample scenes demonstrating UIToolkit usage

See the `UIToolkit` folder for more details and examples.

### Subscribing to Input Events

To subscribe to button clicks and other interactive element events in UI Toolkit, use the standard UI Toolkit event system:

```csharp
// Get reference to your button
var root = uiDocument.rootVisualElement;
var button = root.Q<Button>("MyButton");

// Subscribe to click event (Method 1 - recommended)
button.clicked += OnButtonClicked;

// Or use RegisterCallback (Method 2)
button.RegisterCallback<ClickEvent>(OnButtonClickedEvent);

// Handler methods
private void OnButtonClicked()
{
    Debug.Log("Button clicked!");
}

private void OnButtonClickedEvent(ClickEvent evt)
{
    Debug.Log($"Button clicked at position {evt.position}");
}

// Don't forget to unsubscribe!
private void OnDisable()
{
    button.clicked -= OnButtonClicked;
    button.UnregisterCallback<ClickEvent>(OnButtonClickedEvent);
}
```

**Complete Example**: See `Samples/Meta XR Extensions/Scripts/UIToolkitInputExample.cs` for a comprehensive example showing:
- How to subscribe to button click events
- How to handle text field, toggle, and slider events
- How to use pointer events (hover, enter, leave, etc.)
- Proper event cleanup to prevent memory leaks
- Multiple methods for querying and accessing UI elements

The example includes a matching UXML file (`UIToolkitInputExample.uxml`) demonstrating the UI structure.

## Notes

- BlockData assets can be created manually or programmatically
- When creating blocks programmatically from prefabs, the `CustomBlockDataCreator` API handles all setup automatically
- Deleting a BlockData asset does not delete the associated prefab
- Tags "Internal", "Hidden", and invalid tags are automatically removed from blocks
- Block IDs are GUIDs to ensure uniqueness
- The system automatically refreshes when assets change, but you can manually refresh using the menu item if needed

## Requirements

- Unity Editor (Unity 6000.2 or compatible versions that support Meta XR SDK)
- Meta XR SDK with Building Blocks system installed
- Valid assembly references to:
  - `Meta.XR.BuildingBlocks.Editor`
  - `Meta.XR.BuildingBlocks`
  - `Meta.XR.Editor.Tags`
  - `Meta.XR.Editor.Id`

## Package Information

- **Package Name**: `com.meta.xr.extensions`
- **Display Name**: Meta XR Extensions
- **Version**: 1.0.0
- **Author**: Timur Prokopiev (timur.proko@gmail.com)
