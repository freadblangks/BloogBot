﻿using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace FrostMageBot
{
    class RestState : IBotState
    {
        const string Evocation = "Evocation";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;

        readonly WoWItem foodItem;
        readonly WoWItem drinkItem;

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;

            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);
        }

        public void Update()
        {
            if (player.IsChanneling)
                return;

            if (InCombat || ObjectManager.GetPartyMembers().Any(p => p.IsInCombat))
            {
                player.Stand();
                botStates.Pop();
                return;
            }

            if (HealthOk && ManaOk)
            {
                player.Stand();
                botStates.Pop();
                botStates.Push(new BuffSelfState(botStates, container));
                return;
            }

            if (player.ManaPercent < 20 && player.IsSpellReady(Evocation))
            {
                player.LuaCall($"CastSpellByName('{Evocation}')");
                return;
            }

            if (foodItem != null && !player.IsEating && player.HealthPercent < 80)
                foodItem.Use();

            var drinkItemExists = drinkItem != null;
            var soloCondition = !ObjectManager.IsGrouped && player.ManaPercent < 70;
            var groupedCondition = ObjectManager.IsGrouped && (player.ManaPercent < 30 || (ObjectManager.GetPartyMembers().Any(p => p.IsDrinking) && player.ManaPercent < 70));
            if (drinkItem != null && !player.IsDrinking && (soloCondition || groupedCondition))
                drinkItem.Use();
        }

        bool HealthOk => foodItem == null || player.HealthPercent >= 90 || (player.HealthPercent >= 80 && !player.IsEating);

        bool ManaOk => drinkItem == null || player.ManaPercent >= 90 || (player.ManaPercent >= 80 && !player.IsDrinking) || (ObjectManager.GetPartyMembers().Any(p => p.IsDrinking) && player.ManaPercent >= 90) || (!ObjectManager.GetPartyMembers().Any(p => p.IsDrinking) && player.ManaPercent >= 30 && ObjectManager.IsGrouped);

        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);
    }
}
