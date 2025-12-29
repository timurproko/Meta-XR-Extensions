<img width="1404" height="1101" alt="image" src="https://github.com/user-attachments/assets/e1379eb2-de8f-481c-9c31-552e724c54af" />

# Meta XR Extensions

This package provides extensions for Meta XR SDK, including the ability to create custom Building Blocks that appear in the Meta XR SDK Building Blocks panel.

## Features

- **Custom Building Blocks**: Create custom Building Blocks that integrate seamlessly with Meta XR SDK
- **Easy Creation**: Create Building Block assets with a simple context menu
- **Automatic Integration**: Custom blocks automatically appear in the Building Blocks window alongside Meta's official blocks
- **Management Window**: View and manage all your custom Building Blocks in one place
- **Automatic Injection**: Custom blocks are automatically injected into the Building Blocks registry
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

## UIToolkit Block

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
