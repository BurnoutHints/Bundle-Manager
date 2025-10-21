# Contribution guide

As open-source software, contributions to Bundle Manager are welcome.

## Status

The current version of Neo is little more than a proof-of-concept. If you choose to work on it at this stage, expect breaking changes to be introduced. There are likely a few bugs as well.

## Project overview

The solution is made up of the following:

- **BundleManager**: The core application which contains the UI code.
- **BundleFormat**: Handles bundles and resources.
- **Serialization**: A reflection-based binary deserializer and serializer.
- **Configuration**: Application and file configuration details.
- **Information**: Contains basic information about the games and resource types.

As a contributor, the majority of your work will be centered around adding support for resource types. You will primarily be editing the BundleFormat library and, if creating custom views, the core application. The other libraries are important to understand, but are unlikely to require editing.

## Adding resource types

**Resource types must be documented on the [Burnout Wiki](https://burnout.wiki) prior to being added.**

Two lists of supported resource types are maintained, one for Burnout Paradise and earlier and the other for Need for Speed Hot Pursuit and later. To add a resource type, first create the top-level model class you will use, ensuring it implements ISerializableResourceType. Organize resource type files/folders in BundleFormat's ResourceTypes folder by resource type first and, if necessary, by game, platform, and version.

Once the top-level class has been created, add it to the [resource type class registry](https://github.com/BurnoutHints/Bundle-Manager/blob/neo/BundleFormat/Registry/ResourceTypeClassRegistry.cs) as a unique structure under the resource type's key with its applicable games:

```cs
[ResourceType.ProgressionData] =
[
	new UniqueStructure
	{
		TopLevelClass = typeof(ResourceTypes.ProgressionData.BP_10.ProgressionData),
		ApplicableGames =
		[
			new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_10 },
			new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_13 },
			new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_10 },
			new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_13 }
		]
	},
	// ...
]
```

With this done, you can move on to defining the model and implementing serialization.

## Serialization

Given a correctly annotated model, the serialization library can read and write binary data automatically. See the [serialization guide](SERIALIZATION.md) for details.

## Creating custom views

This is not yet fully supported/tested, though there is a framework in place for it. This section will be updated once at least one custom view has been created.
