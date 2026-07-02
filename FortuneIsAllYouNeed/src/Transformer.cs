using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace FortuneIsAllYouNeed;

public static class Transformer
{
    private static readonly CardRarity[] TargetRarities =
        [CardRarity.Basic, CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare, CardRarity.Ancient];

    public static void TransformAllCards(dynamic player, Logger logger)
    {
        var rng = player.RunState.Rng.Niche;

        var cards = ((IEnumerable<CardModel>)PileTypeExtensions.GetPile(
            PileType.Deck, player).Cards).ToList();
        if (cards.Count == 0)
        {
            logger.Info("No cards to transform.");
            return;
        }

        // Count upgrades among all deck cards before transformation.
        var upgradedCount = cards.Count(c => c.IsUpgraded);
        logger.Info($"{upgradedCount} upgraded cards before transform.");

        var transformations = new List<CardTransformation>();

        foreach (var card in cards)
        {
            if (!TargetRarities.Contains(card.Rarity))
                continue;

            var playerTyped = (Player)player;
            CardModel replacement;

            if (card.Rarity == CardRarity.Ancient)
            {
                // Ancient → Ancient: pick a random card from the pool
                // and create the replacement directly, bypassing
                // GetFilteredTransformationOptions whose FilterForPlayerCount
                // may reject all Ancient candidates.
                var ancientOptions = card.Pool
                    .GetUnlockedCards(playerTyped.UnlockState, playerTyped.RunState.CardMultiplayerConstraint)
                    .Where(c => c.Rarity == CardRarity.Ancient && c.Id != card.Id)
                    .ToList();

                if (ancientOptions.Count == 0)
                    continue;

                var picked = rng.NextItem(ancientOptions);
                replacement = card.CardScope.CreateCard(picked, card.Owner);
            }
            else
            {
                replacement = CardFactory.CreateRandomCardForTransform(card, false, rng);
            }

            transformations.Add(new CardTransformation(card, replacement));
            logger.Info($"Transformed '{card.Title}' -> '{replacement.Title}'");
        }

        if (transformations.Count == 0)
        {
            logger.Info("No cards eligible for transformation.");
            return;
        }

        CardCmd.Transform(transformations, rng, CardPreviewStyle.None)
            .GetAwaiter().GetResult();

        // After transform, upgrade a random selection of N cards to preserve
        // the number of upgraded cards the player had before.
        if (upgradedCount > 0)
        {
            var deckCards = ((IEnumerable<CardModel>)PileTypeExtensions.GetPile(
                PileType.Deck, player).Cards)
                .Where(c => !c.IsUpgraded && TargetRarities.Contains(c.Rarity))
                .OrderBy(_ => Random.Shared.Next())
                .Take(upgradedCount)
                .ToList();

            if (deckCards.Count > 0)
            {
                CardCmd.Upgrade(deckCards, CardPreviewStyle.None);
                logger.Info($"Upgraded {deckCards.Count} cards to preserve upgrade count.");
            }
        }

        logger.Info($"Fortune: {transformations.Count} of {cards.Count} cards transformed.");
    }
}
