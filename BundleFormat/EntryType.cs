namespace BundleFormat
{
    public enum EntryType
    {
        Invalid = -1,

        // Standard types, shared between games
        Texture = 0x0, // Also called: RwRaster, PlaneTexture, TexturePage
        Material = 0x1, // Also called: RwMaterial, MaterialAssembly
        RenderableMesh = 0x2, // Also called: Renderable; likely deprecated
        TextFile = 0x3,
        DrawIndexParams = 0x4,
        IndexBuffer = 0x5,
        MeshState = 0x6,
        TextureAuxInfo = 0x7,
        VertexBufferItem = 0x8,
        VertexBuffer = 0x9,
        VertexDescriptor = 0xA, // Also called: RwVertexDesc
        MaterialCRC32 = 0xB, // Also called: RwMaterialCRC32
        Renderable = 0xC, // Also called: RwRenderable, GtRenderable
        MaterialTechnique = 0xD,
        TextureState = 0xE, // Also called: RwTextureState
        MaterialState = 0xF, // Also called: BlendState
        DepthStencilState = 0x10,
        RasterizerState = 0x11,
        ShaderProgramBuffer = 0x12, // Also called: RwShaderProgramBuffer, PixelShader, VertexShader, ShaderProgramState
        RenderTargetState = 0x13,
        ShaderParameter = 0x14, // Also called: RwShaderParameter
        RenderableAssembly = 0x15,
        Debug = 0x16, // Also called: RwDebug
        KdTree = 0x17,
        VoiceHierarchy = 0x18,
        Snr = 0x19,
        InterpreterData = 0x1A,
        AttribSysSchema = 0x1B,
        AttribSysVault = 0x1C,
        EntryList = 0x1D,
        AptData = 0x1E, // Also called: AptDataHeader, Flash
        Popup = 0x1F, // Also called: GuiPopup
        Font = 0x21,
        LuaCode = 0x22,
        InstanceList = 0x23,
        CollisionMeshData = 0x24, // Also called: ClusteredMesh
        IdList = 0x25, // Also called: ResourceIdList
        InstanceCollisionList = 0x26,
        Language = 0x27,
        SatNavTile = 0x28,
        SatNavTileDirectory = 0x29,
        Model = 0x2A, // Also called: VehicleModel, WheelModel, PropModel
        ColourCube = 0x2B, // Also called: RwColourCube
        HudMessage = 0x2C, // Also called: GuiHudMessage
        HudMessageList = 0x2D,
        HudMessageSequence = 0x2E,
        HudMessageSequenceDictionary = 0x2F,
        WorldPainter2D = 0x30, // Also called: World Painter 2D
        PFXHookBundle = 0x31, // Also called: GuiPFXHook
        ShaderTechnique = 0x32, // Also called: Shader

        RawFile = 0x40, // Also called: RAW, File
        ICETakeDictionary = 0x41, // Also called: ICEDictionary; base type: Dictionary
        VideoData = 0x42,
        PolygonSoupList = 0x43, // Also called: CollisionMeshData
        DeveloperList = 0x44,
        CommsToolListDefinition = 0x45, // Also called: CommsToolDef
        CommsToolList = 0x46, // Also called: CoomsToolInst

        BinaryFile = 0x50, // Also called: AlignedBinaryFile; used as base type
        AnimationCollection = 0x51,

        // Random type IDs from the Black builds, probably for testing
        CharAnimBankFile = 0x2710, // Also called: Character Animation Bank Data
        WeaponFile = 0x2711, // Also called: WEAPONDATA
        VFXFile = 0x343E, // Also called: VFX Data
        BearFile = 0x343F, // Also called: Bear Data
        BkPropInstanceList = 0x3A98,

        // Sound-related types
        Registry = 0xA000,
        GenericRwacFactoryConfiguration = 0xA010,
        GenericRwacWaveContent = 0xA020, // Also called: Generic Wave Content
        GinsuWaveContent = 0xA021, // Also called: Ginsu Wave Content
        AemsBank = 0xA022, // Also called: AEMS Bank
        Csis = 0xA023, // Also called: CSIS
        Nicotine = 0xA024, // Also called: Nicotine Map
        Splicer = 0xA025, // Also called: Splicer Data
        FreqContent = 0xA026,
        VoiceHierarchyCollection = 0xA027,
        GenericRwacReverbIRContent = 0xA028,
        SnapshotData = 0xA029, // Also called: Snapshot Data

        ZoneList = 0xB000,

        // Game-specific types - Burnout Paradise
        LoopModel = 0x10000,
        AISections = 0x10001, // Also called: AIMapData
        TrafficData = 0x10002,
        TriggerData = 0x10003, // Also called: Trigger
        DeformationModel = 0x10004,
        VehicleList = 0x10005,
        GraphicsSpec = 0x10006, // Also called: VehicleGraphics
        PhysicsSpec = 0x10007,
        ParticleDescriptionCollection = 0x10008,
        WheelList = 0x10009,
        WheelGraphicsSpec = 0x1000A, // Also called: WheelGraphics; base type: GraphicsSpec
        TextureNameMap = 0x1000B,
        ICEList = 0x1000C,
        ICEData = 0x1000D, // Also called: ICE
        ProgressionData = 0x1000E, // Also called: Progression
        PropPhysics = 0x1000F,
        PropGraphicsList = 0x10010,
        PropInstanceData = 0x10011, // Also called: PropInstances
        BrnEnvironmentKeyframe = 0x10012, // Base type: Keyframe
        BrnEnvironmentTimeLine = 0x10013, // Base type: TimeLine
        BrnEnvironmentDictionary = 0x10014, // Base type: Dictionary
        GraphicsStub = 0x10015, // Also called: TrafficStub, TrafficGraphicsStub
        StaticSoundMap = 0x10016,
        DEPRECATED_PFXHookBundle = 0x10017, // Replaced by type 0x31 shortly after 2006-11-13
        StreetData = 0x10018,
        VFXMeshCollection = 0x10019, // Also called: BrnVFXMeshCollection
        MassiveLookupTable = 0x1001A,
        VFXPropCollection = 0x1001B,
        StreamedDeformation = 0x1001C, // Also called: StreamedDeformationSpec
        ParticleDescription = 0x1001D,
        PlayerCarColours = 0x1001E,
        ChallengeList = 0x1001F,
        FlaptFile = 0x10020,
        ProfileUpgrade = 0x10021,
        OfflineChallengeList = 0x10022,
        VehicleAnimation = 0x10023,
        BodypartRemapData = 0x10024, // Also called: BodyPartRemapping
        LUAList = 0x10025, // Also called: LUAInst
        LUAScript = 0x10026,

        // Game-specific types - Black 360/Black 2
        BkSoundWeapon = 0x11000, // Also called: SoundWeapon Data
        BkSoundGunsu = 0x11001, // Also called: SoundGunsu Data
        BkSoundBulletImpact = 0x11002, // Also called: SoundBulletImpact Data
        BkSoundBulletImpactList = 0x11003, // Also called: SoundBulletImpactList
        BkSoundBulletImpactStream = 0x11004,  // Also called: SoundBulletImpactStream Data
    }
}
