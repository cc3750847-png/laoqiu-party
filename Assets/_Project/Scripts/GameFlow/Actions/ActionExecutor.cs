using LaoqiuParty.Rules.Dice;

namespace LaoqiuParty.GameFlow.Actions
{
    public sealed class ActionExecutor
    {
        public int ResolveRoll(GameActionRequest request, DiceRoller diceRoller)
        {
            if (request != null && request.intValue > 0)
            {
                return request.intValue;
            }

            return diceRoller != null ? diceRoller.Roll() : 1;
        }

        public int ResolveChoiceIndex(GameActionRequest request, int optionCount)
        {
            if (optionCount <= 0)
            {
                return -1;
            }

            if (request == null)
            {
                return 0;
            }

            if (request.intValue < 0)
            {
                return 0;
            }

            if (request.intValue >= optionCount)
            {
                return optionCount - 1;
            }

            return request.intValue;
        }

        public bool ResolveShopDecision(GameActionRequest request, bool fallbackBuy)
        {
            if (request == null)
            {
                return fallbackBuy;
            }

            return request.boolValue;
        }

        public bool ResolveBooleanDecision(GameActionRequest request, bool fallbackValue)
        {
            if (request == null)
            {
                return fallbackValue;
            }

            return request.boolValue;
        }
    }
}
