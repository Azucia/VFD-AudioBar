# VFD AudioBar v1.0 by Azucia

Unity / VRChat audio visualization assets.

- `Packages/`: original Unity package, preserved unchanged.
- `Assets/`: extracted resources with their original Unity `.meta` files and GUIDs.
- `VFD_Ultimate.cs`: reads left/right output from an assigned Unity AudioSource.
- `VFD_Pro.cs`: reads AudioLink data.

The scripts reference UdonSharp and VRChat SDK; the Pro variant also references AudioLink. These dependencies are not bundled in this package.

Import the original `.unitypackage` into a project with the required dependencies. This upload has not been tested in the Unity Editor or VRChat.
