using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    public static class DroneArrayEdits
    {
        /// <summary>Deleting a row must retain unfinished inputs in all remaining rows.</summary>
        public static void Remove(JArray rows,int index,string path,params Dictionary<string,string>[] states)
        {
            if(index<0 || index>=rows.Count)throw new ArgumentOutOfRangeException(nameof(index));
            string prefix=path+"[";
            foreach(var state in states) {
                var moved=new Dictionary<string,string>();
                foreach(var pair in state.ToArray()) {
                    if(!pair.Key.StartsWith(prefix,StringComparison.Ordinal))continue;
                    int end=pair.Key.IndexOf(']',prefix.Length);
                    if(end<0 || !int.TryParse(pair.Key.Substring(prefix.Length,end-prefix.Length),out int row))continue;
                    state.Remove(pair.Key);
                    if(row!=index)moved[prefix+(row>index?row-1:row)+pair.Key.Substring(end)]=pair.Value;
                }
                foreach(var pair in moved)state[pair.Key]=pair.Value;
            }
            rows.RemoveAt(index);
        }
    }
}
