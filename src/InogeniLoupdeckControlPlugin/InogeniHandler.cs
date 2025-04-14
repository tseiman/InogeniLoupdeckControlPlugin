

namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;


    public class InogeniHandler
    {
        public enum States {
            NoSerial,
            PcUnavailable,
            Inactive,
            Active
        }

      //  private Int32 _pc1state = 0;

        private String SerialDevice { set; get; }

        public States pc1state { get; set;  } = States.NoSerial;
        public States pc2state { get; set;  } = States.NoSerial;

        public void setPC1State() {

         

            this.pc1state = this.GetNextState(this.pc1state);

            PluginLog.Verbose($"InogeniHandler setPC1State {this.pc1state.ToString()}");

        }





        public States GetNextState(States currentState)
        {
            PluginLog.Verbose($"InogeniHandler GetNextState");
            // Get the next state, and loop back to the first state if the last state is reached
            Array values = Enum.GetValues(typeof(States));
            Int32  nextIndex = (Array.IndexOf(values, currentState) + 1) % values.Length;
            return (States)values.GetValue(nextIndex);
        }


    }
}

