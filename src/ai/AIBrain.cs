using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class AIBrain
{
    public Player Player { get; private set; }

    private static float minimumTurnProcessTime = .5f;
    private bool isMyTurn;
    private float turnDelta;

    private bool hasChoosenStartingRegion;

    private Dictionary<ulong, Card> cards;
    private Dictionary<int, Region> regions;

    private Dictionary<int, Region> freeRegions;

    public AIBrain(Player player)
    {
        Player = player;
        cards = new Dictionary<ulong, Card>();
        regions = new Dictionary<int, Region>();
        freeRegions = new Dictionary<int, Region>();

        Card.OnDeath += HandleCardDeath;
        Card.OnCreated += HandleCardCreated;
        Region.OnRegionOccupied += HandleRegionOccupied;
        TurnManager.OnWaitingForPlayer += HandleTurnWaitingForPlayer;
    }

    ~AIBrain()
    {
        Card.OnDeath -= HandleCardDeath;
        Card.OnCreated -= HandleCardCreated;
        Region.OnRegionOccupied -= HandleRegionOccupied;
        TurnManager.OnWaitingForPlayer -= HandleTurnWaitingForPlayer;
    }

    public void Process(float delta)
    {
        if (isMyTurn)
        {
            turnDelta += delta;

            if (turnDelta >= minimumTurnProcessTime)
            {
                isMyTurn = false;
                turnDelta = 0;
                HandleMyTurn();
            }
        }
    }

    private void HandleTurnWaitingForPlayer(int turn, Player previousPlayer, Player currentPlayer)
    {
        if (!Player.IsBot) return;
        if (currentPlayer != Player) return;
        isMyTurn = true;
    }

    private void HandleMyTurn()
    {
        if (!hasChoosenStartingRegion)
        {
            ChooseStartingRegion();
            Player.RequestTurnEnd();
            return;
        }
        if (freeRegions.Count > 0)
        {
            Card card = GetRandomCard(cards.Values.ToList(), nextToFreeRegion:true);
            Region region = GetRandomRegion(freeRegions.Values.ToList());
            if (card != null)
            {
                card.SetRegion(region);
                Player.RequestTurnEnd();
                return;
            }
        }

        Player.RequestTurnEnd();
    }

    private void ChooseStartingRegion()
    {
        if (hasChoosenStartingRegion) return;

        Region region = RegionManager.GetRandomUnoccupiedRegion();
        if (region == null) return;

        region.SetOccupier(Player);
        CardManager.CreateRandomCard(Player.Id, region.Id);
        hasChoosenStartingRegion = true;
    }

    private Region GetRandomRegion(List<Region> regions, Card nextToCard = null)
    {
        if (nextToCard != null)
        {
            regions = regions.Where(x => nextToCard.Region.Neighbours.Contains(x.Id)).ToList();
        }
        if (regions.Count == 0)
        {
            GD.PushError($"Given regions list was empty after the filters.");
            return null;
        }

        Random random = new Random();
        int rng = random.Next(regions.Count);
        return regions[rng];
    }

    private Card GetRandomCard(List<Card> cards, Region inRegion = null, Region nextToRegion = null, bool nextToFreeRegion = false)
    {
        if (inRegion != null)
        {
            cards = cards.Where(x => x.Region == inRegion).ToList();
        }
        if (nextToRegion != null)
        {
            cards = cards.Where(x => x.Region.Neighbours.Contains(nextToRegion.Id)).ToList();
        }
        if (nextToFreeRegion)
        {
            cards = cards.Where((x) => {
                foreach (var id in x.Region.Neighbours)
                {
                    if (freeRegions.ContainsKey(id))
                    {
                        return true;
                    }
                }
                return false;
            }).ToList();
        }
        if (cards.Count == 0)
        {
            GD.PushError($"Given cards list was empty after the filters.");
            return null;
        }

        Random random = new Random();
        int rng = random.Next(cards.Count);
        return cards[rng];
    }

    private void HandleCardDeath(Card card)
    {
        cards.Remove(card.Id);
    }

    private void HandleCardCreated(Card card)
    {
        if (card.Holder != Player) return;
        cards.Add(card.Id, card);
    }

    private void HandleRegionOccupied(Region region, Player player)
    {
        if (regions.Remove(region.Id))
        {
            // if this was in my regions and no longer is
            // we might have its neighbours as free regions
            // in that case we might want to remove them
            foreach (var id in region.Neighbours)
            {
                if (freeRegions.TryGetValue(id, out var neighbour))
                {
                    bool isEligible = false;
                    // this was in free regions
                    // and it might still be an eligible one
                    foreach (var _id in neighbour.Neighbours)
                    {
                        if (regions.ContainsKey(_id)) isEligible = true;
                    }

                    if (!isEligible) freeRegions.Remove(id);
                }
            }
        }
        freeRegions.Remove(region.Id);
        
        if (player != Player) return;

        regions.Add(region.Id, region);
        foreach (var id in region.Neighbours)
        {
            Region neighbour = RegionManager.GetRegion(id);
            if (neighbour.Occupier != null) continue;
            if (freeRegions.ContainsKey(neighbour.Id)) continue;
            freeRegions.Add(neighbour.Id, neighbour);
        }
    }
}
