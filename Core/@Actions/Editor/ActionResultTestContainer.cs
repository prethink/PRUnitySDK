#if PRSDK_TESTS
public class ActionResultTestContainer : InlineActionContainer
{
    public override ActionResult CanExecute() => InnerAction.CanExecute();
}
#endif
