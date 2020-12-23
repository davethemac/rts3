using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Gatherer : MonoBehaviour
{
    public event Action<int> OnGatheredChanged;

    [SerializeField] private int _maxCarried = 20;
    [SerializeField] private ResourceType _resourceType = ResourceType.Fruit;

    private StateMachine _stateMachine;
    private int _gathered;

    public ResourceType GetResourceType()
    {
        return _resourceType;
    }

    public GatherableResource Target { get; set; }
    public StockPile StockPile { get; set; }

    public float moveSpeed = 1.0f;

    private void Awake()
    {
        var navMeshAgent = GetComponent<NavMeshAgent>();
        // var animator = GetComponent<Animator>();
        var enemyDetector = gameObject.AddComponent<EnemyDetector>();
        // var fleeParticleSystem = gameObject.GetComponentInChildren<ParticleSystem>();

        _stateMachine = new StateMachine();

        // create the states used by this entity
        var search = new SearchForResource(this);
        // var moveToSelected = new MoveToSelectedResource(this, navMeshAgent, animator);
        var moveToSelected = new MoveToSelectedResource(this, navMeshAgent);
        // var harvest = new HarvestResource(this, animator);
        var harvest = new HarvestResource(this);
        // var returnToStockpile = new ReturnToStockpile(this, navMeshAgent, animator);
        var returnToStockpile = new ReturnToStockpile(this, navMeshAgent);
        var placeResourcesInStockpile = new PlaceResourcesInStockpile(this);
        // var flee = new Flee(this, navMeshAgent, enemyDetector, animator, fleeParticleSystem);
        // var flee = new Flee(this, navMeshAgent, enemyDetector, animator, fleeParticleSystem);
        var flee = new Flee(this, navMeshAgent, enemyDetector);

        // state transitions
        At(search, moveToSelected, HasTarget());
        At(moveToSelected, search, StuckForOverASecond());
        At(moveToSelected, harvest, ReachedResource());
        At(harvest, search, TargetIsDepletedAndICanCarryMore());
        At(harvest, returnToStockpile, InventoryFull());
        At(returnToStockpile, placeResourcesInStockpile, ReachedStockpile());
        At(placeResourcesInStockpile, search, () => _gathered == 0);

        _stateMachine.AddAnyTransition(flee, () => enemyDetector.EnemyInRange);
        At(flee, search, () => enemyDetector.EnemyInRange == false);

        _stateMachine.SetState(search);

        // local helper functions
        void At(IState to, IState from, Func<bool> condition) => _stateMachine.AddTransition(to, from, condition);
        // predicates
        Func<bool> HasTarget() => () => Target != null;
        Func<bool> StuckForOverASecond() => () => moveToSelected.TimeStuck > 1f;
        Func<bool> ReachedResource() => () => Target != null &&
                                              Vector3.Distance(transform.position, Target.transform.position) < 1.6f;
        /*
        Func<bool> ReachedResource() => () =>
        {
            if(Target != null)
            {
                float dist = Vector3.Distance(transform.position, Target.transform.position);
                if(dist < 1f)
                {
                    return true;
                }
            }
            return false;
        };*/

        Func<bool> TargetIsDepletedAndICanCarryMore() => () => (Target == null || Target.IsDepleted) && !InventoryFull().Invoke();
        Func<bool> InventoryFull() => () => _gathered >= _maxCarried;
        Func<bool> ReachedStockpile() => () => StockPile != null &&
                                               Vector3.Distance(transform.position, StockPile.transform.position) < 3f;

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _stateMachine.Tick();
    }

    /*
     *  used by the HarvestResource state
     *  conceptually IResourceHarvester 
     *  public GatherableResource Target { get; set; }
     *  public void TakeFromTarget()
     *  and the HarvestResource constructor could be
     *  public HarvestResource(IResourceHarvester harvester)
     *  instead of
     *  public HarvestResource(Gatherer gatherer)
     */
    public void TakeFromTarget()
    {
        if (Target.Take())
        {
            _gathered++;
            OnGatheredChanged?.Invoke(_gathered);
        }
    }

    /*
     * used by PlaceResourcesInStockpile
     * could have an interface along the same lines as in preceeding method
     */
    public bool Take()
    {
        if (_gathered <= 0)
            return false;

        _gathered--;
        OnGatheredChanged?.Invoke(_gathered);
        return true;
    }

    /*
     * used by Flee state
     * this is a bit harder to neatly abstract, 
     * as not all entities that might flee will be gatherers
     * might rename this 'Panic' or something
     */
    public void DropAllResources()
    {
        if (_gathered > 0)
        {
            FindObjectOfType<WoodDropper>().Drop(_gathered, transform.position);
            _gathered = 0;
            OnGatheredChanged?.Invoke(_gathered);
        }
    }
}
