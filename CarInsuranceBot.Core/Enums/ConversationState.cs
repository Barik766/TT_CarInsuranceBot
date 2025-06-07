using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceBot.Core.Enums
{
    public enum ConversationState
    {
        Start,
        WaitingPassport,
        WaitingCarDoc,
        WaitingConfirmation,
        PriceConfirmation,
        GeneratingPolicy,
        Completed,
        Reset,
        Error
    }
}
