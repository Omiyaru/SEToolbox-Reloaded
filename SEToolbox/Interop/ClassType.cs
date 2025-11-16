namespace SEToolbox.Interop
{
    public enum ClassType
    {
        Unknown,
        Character,
        FloatingObject,
        LargeShip,
        LargeStation,
        SmallShip,
        SmallStation,
        Meteor,
        Voxel,
        Planet,
        InventoryBag
    };

    public enum ImportModelClassType
    {
        SmallShip,
        SmallStation,
        LargeShip,
        LargeStation,
        Asteroid
    };

    public enum ImportImageClassType
    {
        SmallShip,
        SmallStation,
        LargeShip,
        LargeStation,
    };

    public enum ImportArmorType
    {

        Heavy, Light,
        Round, Angled,
        Corner, Slope,
        HeavyRounded, LightRounded,
        HeavyAngled, LightAngled,
        HeavyCorner, LightCorner,
        HeavySlope, LightSlope



    };
    
    public enum Materials
    {
        Ice,
        Stone,
        Nickel,
        Iron,
        Platnum,
        Cobalt,
        Silicon,
        Silver,
        Uranium,
    };
}
