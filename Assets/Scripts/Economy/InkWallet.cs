using System;
using UnityEngine;

namespace ClickerGenesis.Economy
{
    /// <summary>
    /// Holds the player's Ink balance (the game's core tap/idle currency).
    /// Plain C# class so it can be unit-tested and owned by any MonoBehaviour.
    /// </summary>
    [Serializable]
    public class InkWallet
    {
        [SerializeField] private double balance;

        public double Balance => balance;

        public event Action<double> OnBalanceChanged;

        public InkWallet(double startingBalance = 0)
        {
            balance = Math.Max(0, startingBalance);
        }

        public void Add(double amount)
        {
            if (amount <= 0) return;
            balance += amount;
            OnBalanceChanged?.Invoke(balance);
        }

        public bool CanAfford(double cost)
        {
            return balance >= cost;
        }

        /// <summary>Attempts to spend Ink. Returns false (no-op) if the balance is insufficient.</summary>
        public bool TrySpend(double cost)
        {
            if (cost < 0 || !CanAfford(cost)) return false;
            balance -= cost;
            OnBalanceChanged?.Invoke(balance);
            return true;
        }

        /// <summary>Resets the balance to zero — used by the opt-in prestige reset path.</summary>
        public void ResetBalance()
        {
            balance = 0;
            OnBalanceChanged?.Invoke(balance);
        }
    }
}
