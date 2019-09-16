using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1x3_Array_Force_Sensor
{
    public class MovingAverage
    {

        private Queue<int> samples = new Queue<int>();
        private int windowSize = 5;
        private int sampleAccumulator;
        public int Average { get; private set; }

        public void ComputeAverage(int newSample)
        {
            sampleAccumulator += newSample;
            samples.Enqueue(newSample);

            if (samples.Count > windowSize)
            {
                sampleAccumulator -= samples.Dequeue();
            }

            Average = sampleAccumulator / samples.Count;
        }
        
    }
}
