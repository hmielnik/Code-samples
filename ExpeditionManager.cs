using CityBuilderCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ExpeditionManager : MonoBehaviour
{
    public static ExpeditionManager Instance;
    [SerializeField] private ExpeditionListingScriptable ExpeditionListing;
    private IGameSpeed _gameSpeed;
    [SerializeField] private List<int> ExpeditionIDsLeft = new List<int>();

    [Header("Expedition Info")]
    public bool isExpeditionInProgress = false;
    public int ExpeditionID = 0;
    public float ExpeditionEndTime;

    [Header("LookUp")]
    public float currentTime = 0;

    public event Action<Listing,int> onExpeditionListingUILoadEvent;
    public event Action<int> onExpeditionStartedEvent;
    public event Action<int> onExpeditionEndEvent;
    public event Action<int> onExpeditionGatherRewardEvent;
    public event Action onExpeditionCancelEvent;
    


    public void Awake()
    {
        Instance = this;
        foreach (Listing listing in ExpeditionListing.Expeditions)
        {
            ExpeditionIDsLeft.Add(listing.id);
        }
        ExpeditionIDsLeft.Shuffle();
    }

    private void Start()
    {
       _gameSpeed = Dependencies.Get<IGameSpeed>();    
    }

    private void Update()
    {
        currentTime += Time.deltaTime;
        if(isExpeditionInProgress)
        {
            if(currentTime >= ExpeditionEndTime)
            {
                OnExpeditionEnd(ExpeditionID);
            }
        }
    }

    public void ProvideExpeditionListings(int listingAmount = 2)
    {
        if(ExpeditionIDsLeft.Count < listingAmount)
        {
            listingAmount = ExpeditionIDsLeft.Count;
        }

        for(int i = 0; i < listingAmount; i++)
        {
            onExpeditionListingUILoadEvent.Invoke(ExpeditionListing.Expeditions[ExpeditionIDsLeft[i]], isExpeditionInProgress ? (int)ButtonModes.OtherExpeditionInProgressButton : (int)ButtonModes.StartExpeditionButton);
        }
        for (int i = 0; i < listingAmount; i++)
        {
            ExpeditionIDsLeft.RemoveAt(0);
        }
    }

    public void OnExpeditionEnd(int id)
    {
        isExpeditionInProgress = false;
        onExpeditionEndEvent.Invoke(id);
    }

    public void OnExpeditionStarted(int id)
    {
        onExpeditionStartedEvent.Invoke(id);
        ExpeditionID = id;
        isExpeditionInProgress = true;
        ExpeditionEndTime = currentTime + ExpeditionListing.Expeditions[id].Time;
    }

    public void OnExpeditionGatherReward(int id)
    {
        int state = isExpeditionInProgress ? (int)ButtonModes.OtherExpeditionInProgressButton : (int)ButtonModes.StartExpeditionButton;
        onExpeditionGatherRewardEvent.Invoke(id,state);
        onExpeditionGatherRewardEvent.Invoke(id);
        foreach (ItemRewards item in ExpeditionListing.Expeditions[id].Reward)
        {
            defaultItemManager.ItemStorage.AddItems(item.Item, item.Quantity);
            float quantity = (float)Variables.Application.Get(item.id) + item.amount;
            Variables.Application.Set(item.id, quantity);
        }
    }

    public void OnExpeditionCancel()
    {
        onExpeditionCancelEvent();
        isExpeditionInProgress = false;
    }
}
