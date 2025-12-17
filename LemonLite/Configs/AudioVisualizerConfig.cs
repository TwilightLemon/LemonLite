using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonLite.Configs;

public class AudioVisualizerConfig
{
    public double Opacity { get; set; } = 0.6;
    public bool EnableBorderRendering { get; set; } = false;
    public bool EnableStripsRendering {  get; set; } = true;
}
