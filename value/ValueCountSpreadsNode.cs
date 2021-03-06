#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Values
{
	#region PluginInfo
	[PluginInfo(Name = "CountSpreads", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueCountSpreadsNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = 1.0)]
		public ISpread<ISpread<double>> FInput;

		[Output("Output")]
		public ISpread<ISpread<double>> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;

			for (int i = 0; i < SpreadMax; i++)
			{
				FOutput[i].SliceCount = FInput[i].SliceCount;
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
