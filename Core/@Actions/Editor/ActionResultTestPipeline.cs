#if PRSDK_TESTS
public class ActionResultTestPipeline : InlineActionPipeline
{
    public ActionResult Availability = ActionResult.Success;
    public override ActionResult CanExecute() => Availability;
}
#endif
