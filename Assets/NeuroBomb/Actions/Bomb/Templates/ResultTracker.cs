public sealed class ResultTracker
{
    private readonly BombComponent component;
    private readonly StrikeEvent onStrike;
    private readonly PassEvent onPass;

    private bool struck;
    private bool solved;

    public ResultTracker(BombComponent component)
    {
        this.component = component;

        onStrike = source => {
            if (source == component) struck = true;
            return true;
        };

        onPass = source => {
            if (source == component) solved = true;
            return true;
        };

        component.OnStrike += onStrike;
        component.OnPass += onPass;
    }

    public string Read()
    {
        component.OnStrike -= onStrike;
        component.OnPass -= onPass;

        if (component.Bomb != null &&
            component.Bomb.HasDetonated)
        {
            return "Bomb exploded!";
        }

        if (component.Bomb != null &&
            component.Bomb.IsSolved())
        {
            return "Bomb defused!";
        }

        if (struck) return "Strike!";

        if (solved) return "Module solved!";

        return "The module remains unsolved.";
    }
}