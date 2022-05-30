using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class Battle
{
    public enum BattleSide { Attacker, Defender }

    public static readonly byte MELEE_RANGE = 5;
    public static readonly byte BATTLE_WIDTH = 5;

    public static event Action<Region, Player, Player> OnAttackerWin; // Region, Defender, Attacker

    public string Name => $"Battle of {Region.Name} in {TurnManager.Instance.Turn}.";

    public Region Region { get; private set; }
    public Dictionary<ulong, Card> Attackers { get; private set; }
    public Dictionary<ulong, Card> Defenders { get; private set; }

    public Player Attacker { get; private set; }
    public Player Defender { get; private set; }

    public Battle(Region region, Dictionary<ulong, Card> attackers, Dictionary<ulong, Card> defenders)
    {
        if (attackers.Count > BATTLE_WIDTH || defenders.Count > BATTLE_WIDTH)
        {
            GD.PushWarning($"At least one side of the battle {Name} was bigger than allowed size {BATTLE_WIDTH}. Battle will simulate, this is just a warning.");
        }

        Region = region;

        Attackers = attackers;
        Defenders = defenders;
        Attacker = Attackers[Attackers.Keys.ToArray()[0]].Holder;
        Defender = Defenders[Defenders.Keys.ToArray()[0]].Holder;

        SetDefendersDefending();
    }

    public bool Simulate()
    {
        List<ulong> deadAttackers = new List<ulong>();
        List<ulong> keys = Attackers.Keys.ToList();
        for (int i = keys.Count - 1; i >= 0; i--)
        {
            Card attacker = Attackers[keys[i]];

            if (attacker.State == Card.STATE.Idle)
            {
                SetTarget(attacker);
            }
            if (attacker.State == Card.STATE.Attacked)
            {
                ulong attackerId = attacker.Id;
                ulong defenderId = attacker.Target.Id;

                bool attackerIsDead = attacker.TakeDamage(attacker.Target);
                bool defenderIsDead = attacker.Target.TakeDamage(attacker);

                if (defenderIsDead)
                {
                    Defenders.Remove(defenderId);
                    if (attacker.State != Card.STATE.Dying)
                        attacker.SetState(Card.STATE.Idle);
                }
                else
                {
                    if (attacker.State != Card.STATE.Dying)
                    {
                        attacker.SetState(Card.STATE.Retrieve);
                    }
                }

                if (attackerIsDead)
                {
                    deadAttackers.Add(attackerId);
                }
            }
            break;
        }

        foreach (var id in deadAttackers) Attackers.Remove(id);

        if (Attackers.Count == 0)
        {
            SetDefendersIdle();
            return true;
        }
        if (Defenders.Count == 0)
        {
            OnAttackerWin?.Invoke(Region, Defender, Attacker);
            return true;
        }
        return false;
    }

    private void SetTarget(Card card)
    {
        Card target = GetClosestCardTo(card, Defenders.Values.ToList());
        card.SetTarget(target);
    }

    private Card GetClosestCardTo(Card to, List<Card> cards)
    {
        if (cards.Count == 0) return null;
        Card closestCard = null;
        float closestDistance = float.MaxValue;
        foreach (var card in cards)
        {
            float distance = to.GlobalPosition.DistanceSquaredTo(card.GlobalPosition);
            if (distance < closestDistance)
            {
                closestCard = card;
                closestDistance = distance;
            }
        }
        return closestCard;
    }

    private void SetDefendersDefending()
    {
        foreach (var defender in Defenders.Values)
        {
            defender.SetState(Card.STATE.Defending);
        }
    }

    private void SetDefendersIdle()
    {
        foreach (var defender in Defenders.Values)
        {
            if (defender.State != Card.STATE.Dying)
                defender.SetState(Card.STATE.Idle);
        }
    }
}
