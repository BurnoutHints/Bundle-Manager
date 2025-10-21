# Bundle Manager Neo

A tool that enables editing of resources in Criterion Games' Bundle archives.

## Purpose

There were several goals in creating Neo, with the main ones being:

- **Universal editor**: Everything should be editable in a single, universal editor which requires no implementation effort on the part of the contributor and which behaves consistently for the user.
- **Automated serialization**: Deserializion and serialization should be done automatically, requiring as little custom code as possible.
- **Support for all bundles**: Bundles from every version of every game on every platform should be supported.
- **Cross-platform support**: Have support for Windows, MacOS and Linux.

In a nutshell, it is meant to make editing resources easy for users and to make adding support for resource types easy for contributors.

## Game support

- Burnout Paradise
- Need for Speed Hot Pursuit (2010)
- Need for Speed Most Wanted (2012)
- Development builds of Burnout 5
- Development builds of Black 2

## Resource type support

Resource types are supported on a per-game, per-platform, per-version basis. See the [resource type class registry](https://github.com/BurnoutHints/Bundle-Manager/blob/neo/BundleFormat/Registry/ResourceTypeClassRegistry.cs) for a list of supported types.

## Usage

Start Bundle Manager by launching the BundleManager executable. On first launch, you'll be required to provide information about the game being edited, including its location, platform, and version. Once configured, it will open to a blank screen with the game's filesystem on the left.

<img width="600" alt="Bundle Manager Neo" src="https://github.com/user-attachments/assets/c7ed953c-cee0-4c61-b9f3-fcce3555ff0a" />

Double click a bundle file to open it. The resource list will be displayed.

<img width="600" alt="Bundle and resource tabs" src="https://github.com/user-attachments/assets/b988c95c-e26e-4554-ba7e-f89c99ff5628" />

Double click a resource to open its editor. From here, you can make changes to the resource.

<img height="300" alt="Progression resource editor" src="https://github.com/user-attachments/assets/9a2bf5f7-1c16-425b-a034-3bc5c38e4cad" />

Once you're done, save the resource using the save button in its tab header. You may also use the file tab's save button, which will save all resources currently being edited, or the save/save as menu items to do the same thing.

<img width="600" alt="Directions for saving" src="https://github.com/user-attachments/assets/31710946-6b15-4308-9d2f-abb16ab75f88" />

## Contributing

Contributions by any developer are welcome. See the [contribution guide](https://github.com/BurnoutHints/Bundle-Manager/blob/neo/CONTRIBUTING.md) for details.
