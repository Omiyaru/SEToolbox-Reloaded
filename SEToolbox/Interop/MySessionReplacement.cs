using Sandbox.Game.Multiplayer;

namespace SEToolbox.Interop
{
    public class MySession
    {
        private MySession(MySyncLayer syncLayer, bool registerComponents = true)
        {
            // Dummy replacement for the Sandbox.Game.World.MySession constructor of the same parameters.
            // So we can create it without getting involved with Havok and other depdancies.
            //possibly in the future ??
        }
    }
}
