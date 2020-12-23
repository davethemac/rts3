using UnityEngine;
internal class PlaceResourcesInStockpile : IState
{
    private readonly Gatherer _gatherer;

    public PlaceResourcesInStockpile(Gatherer gatherer)
    {
        _gatherer = gatherer;
    }

    public void Tick()
    {
        if (_gatherer.Take())
            _gatherer.StockPile.Add();
    }

    public void OnEnter() {
        Debug.Log("Dropping off Resources");
    }

    public void OnExit() { }
}