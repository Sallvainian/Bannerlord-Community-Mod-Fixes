using TaleWorlds.MountAndBlade;

namespace RelentlessSmithConciseBKReduxFixes
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            PatchCoordinator.Apply();
        }

        protected override void OnSubModuleUnloaded()
        {
            PatchCoordinator.Unapply();
            base.OnSubModuleUnloaded();
        }
    }
}
