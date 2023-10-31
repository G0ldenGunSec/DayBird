using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;


namespace NHAPI
{
    public static class OutputRetrieval
    {
        //stores output associated with commands sent from plugins
        public static ConcurrentDictionary<Guid, string> returnedJobOutput = new ConcurrentDictionary<Guid, string>();
                
        
        //GUID vals associated with jobs waiting to have output retrieved are added here
        //for collections that have multiple parts (e.g., inproc-execute-assembly) don't remove from this collection until all collection is complete        
        public static SynchronizedCollection<Guid> activeScriptJobs = new SynchronizedCollection<Guid>();

        static OutputRetrieval()
        {
            returnedJobOutput.TryAdd(Guid.Empty, "Returned value associated with an empty GUID, command was likely not registered correctly. Time to dig through some source code :)");
        }


        public static string GetJobOutput(Guid jobID, bool waitForAllOutput = true, int timeoutVal = 30)
        {
            string retVal = "";
            //default behavior, will sleep up to the timeoutVal checking to see if job has completed
            if(waitForAllOutput)
            {
                //will check once a second to see if target job still exists in active jobs collection
                for(int timer = 0; timer < timeoutVal; timer++)
                {
                    if(activeScriptJobs.Contains(jobID))
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        timer = timeoutVal + 1;
                    }
                }
                //if job is still active at end of timeout period append a disclaimer
                if (activeScriptJobs.Contains(jobID))
                {
                    retVal = "***Job has not yet finished, below may be only be partial output***\r\n";
                }
            }
            //if plugin attempts to get output immediately when this method is called but job is still running, append a disclaimer
            else if (activeScriptJobs.Contains(jobID))
            {
                retVal = "***Job has not yet finished, below may be only be partial output***\r\n";
            }

            if(returnedJobOutput.ContainsKey(jobID))
            {
                retVal += returnedJobOutput[jobID];
            }

            return retVal;
        }

        public static Dictionary<Guid,string> GetJobOutputBulk(List<Guid> jobIDs, bool waitForAllOutput = true, int timeoutVal = 30)
        {
            Dictionary<Guid, string> jobOutputVals = new Dictionary<Guid, string>();

            //populate return obj
            foreach (Guid jobId in jobIDs)
            {
                jobOutputVals.Add(jobId, "");
            }

            //default behavior, will sleep up to the timeoutVal checking to see if job has completed
            if (waitForAllOutput)
            {
                for (int timer = 0; timer < timeoutVal; timer++)
                {
                    bool anyActive = false;

                    foreach (Guid jobID in jobIDs)
                    {
                        if (activeScriptJobs.Contains(jobID))
                        {
                            anyActive = true;
                        }
                    }

                    if (anyActive)
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        timer = timeoutVal + 1;
                    }
                }
                //if job is still active at end of timeout period append a disclaimer
                foreach (Guid jobID in jobIDs)
                {
                    if (activeScriptJobs.Contains(jobID))
                    {
                        jobOutputVals[jobID] = "***Job has not yet finished, below may be only be partial output***\r\n";
                    }

                }
            }
            //append job output here
            foreach (Guid jobID in jobIDs)
            {
                //if plugin attempts to get output immediately when this method is called but job is still running, append a disclaimer to each still-active job
                if (!waitForAllOutput && activeScriptJobs.Contains(jobID))
                {
                    jobOutputVals[jobID] = "***Job has not yet finished, below may be only be partial output***\r\n";
                }
                if (returnedJobOutput.ContainsKey(jobID))
                {
                    jobOutputVals[jobID] += returnedJobOutput[jobID];
                }
            }

            return jobOutputVals;
        }
    }
}
