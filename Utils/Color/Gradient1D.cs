using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class Gradient1D<V> : Gradient<float, V>
    {
        public override float Min
        {
            get
            {
                if(stops.Count > 0)
                {
                    float minPosition = stops[0].position;
                    for(int i = 1; i < stops.Count; ++i)
                    {
                        if(stops[i].position < minPosition)
                        {
                            minPosition = stops[i].position;
                        }
                    }
                    return minPosition;
                }
                else
                {
                    return 0;
                }
            }
        }
        public override float Max
        {
            get
            {
                if(stops.Count > 0)
                {
                    float maxPosition = stops[0].position;
                    for(int i = 1; i < stops.Count; ++i)
                    {
                        if(stops[i].position > maxPosition)
                        {
                            maxPosition = stops[i].position;
                        }
                    }
                    return maxPosition;
                }
                else
                {
                    return 0;
                }
            }
        }

        public override V Sample(float position)
        {
            throw new NotImplementedException();
        }
    }
}
