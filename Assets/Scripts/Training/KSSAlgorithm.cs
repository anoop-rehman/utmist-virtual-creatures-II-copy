using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Text;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace KSS
{
    [System.Serializable]
    public class KSSSave : TrainingSave
    {
        [HideInInspector]
        public List<Generation> generations;

        public CreatureGenotypeEval best;

        // Data on creatures goes here TODO

        public void ExportCSV(){
            StringBuilder sb = new StringBuilder("Generation,Best,Median,Worst");
            for (int i = 0; i < generations.Count; i++)
            {
                Generation g = generations[i];
                sb.Append('\n').Append(i.ToString()).Append(',').Append(g.GetDataString());
            }

            // Use the CSV generation from before
            var content = sb.ToString();

            // The target file path e.g.

            var filePath = Path.Combine(OptionsPersist.instance.VCData, saveName + ".csv");

            using (var writer = new StreamWriter(filePath, false))
            {
                writer.Write(content);
            }

            // Or just
            //File.WriteAllText(content);

            Debug.Log($"CSV file written to \"{filePath}\"");
        }

        public bool IsValid(){
            KSSSettings optimizationSettings = (KSSSettings)ts.optimizationSettings;
            CreatureGenotype initialGenotype = optimizationSettings.initialGenotype;
            if (initialGenotype == null) return true;
            Dictionary<byte, byte> recursiveLimitInitial = new Dictionary<byte, byte>();
            foreach (SegmentGenotype segment in initialGenotype.segments) recursiveLimitInitial[segment.id] = segment.recursiveLimit;

            int segmentCount = 0;
            initialGenotype.IterateSegment(recursiveLimitInitial, null, new List<byte>(), ref segmentCount);
            return segmentCount <= optimizationSettings.mp.maxSegments;
        }
    }

    [System.Serializable]
    public class Generation {
        public List<CreatureGenotypeEval> cgEvals;
        public bool complete { get; private set; }
        public bool culled { get; private set; }
        public float worstReward;
        public float medianReward;
        public float bestReward;

        public void Cull(int populationSize, float survivalRatio)
        {
            if (cgEvals != null && complete && !culled) {
                List<CreatureGenotypeEval> cleanedEvals = new List<CreatureGenotypeEval>(cgEvals);
                cleanedEvals.RemoveAll(x => x.evalStatus == EvalStatus.DISQUALIFIED);
                cleanedEvals.RemoveAll(x => x.fitness.HasValue == false);
                cleanedEvals = cleanedEvals.OrderByDescending(cgEval => cgEval.fitness.Value).ToList();
                bestReward = SelectBestEval().fitness.Value;
                medianReward = cleanedEvals[cleanedEvals.Count / 2].fitness.Value;
                worstReward = cleanedEvals.Last().fitness.Value;

                cgEvals = new List<CreatureGenotypeEval>(SelectTopEvals(populationSize, survivalRatio));
                culled = true;
            }
        }

        public string GetDataString(){
            return string.Format("{0},{1},{2}", bestReward, medianReward, worstReward);
        }

        public CreatureGenotypeEval SelectBestEval()
        {
            List<CreatureGenotypeEval> cleanedEvals = new List<CreatureGenotypeEval>(cgEvals);
            cleanedEvals.RemoveAll(x => x.evalStatus == EvalStatus.DISQUALIFIED);
            cleanedEvals.RemoveAll(x => x.fitness.HasValue == false);
            CreatureGenotypeEval bestEval = cleanedEvals.OrderByDescending(cgEval => cgEval.fitness.Value).FirstOrDefault();
            Debug.Log("Best: " + bestEval.fitness.Value);
            return bestEval;
        }

        public List<CreatureGenotypeEval> SelectTopEvals(int populationSize, float survivalRatio)
        {
            List<CreatureGenotypeEval> cleanedEvals = new List<CreatureGenotypeEval>(cgEvals);
            List<CreatureGenotypeEval> topEvals = new List<CreatureGenotypeEval>();
            cleanedEvals.RemoveAll(x => x.evalStatus == EvalStatus.DISQUALIFIED);
            cleanedEvals.RemoveAll(x => x.fitness.HasValue == false);
            List<CreatureGenotypeEval> sortedEvals = cleanedEvals.OrderByDescending(x => x.fitness.Value).ToList();

            int topCount = Mathf.RoundToInt(populationSize * survivalRatio);
            int positiveCount = 0;
            for (int i = 0; i < topCount; i++)
            {
                CreatureGenotypeEval eval = sortedEvals[i];
                // TEMPORARY FIX: Setting lower boudn to -8 instead of 0. TODO lol
                if (eval.evalStatus == EvalStatus.EVALUATED && eval.fitness != null && eval.fitness.Value >= -8)
                {
                    topEvals.Add(eval);
                    positiveCount++;
                }
            }

            //Debug.Log(string.Format("{0}/{1} Creatures with >=0 fitness.", positiveCount, topCount));

            return topEvals;
        }

        /// <summary>
        /// Creates a new generation w/ size and mutation
        /// </summary>
        /// <param name="size"></param>
        /// <param name="initialGenotype"></param>
        /// <param name="mps"></param>
        public Generation(){
            cgEvals = new List<CreatureGenotypeEval>();
            culled = false;
            complete = false;
        }

        public void Complete(){
            complete = true;
        }

        public static Generation FromInitial(int size, CreatureGenotype initialGenotype, MutateGenotype.MutationPreferenceSetting mp){
            Generation g = new Generation();
            g.cgEvals = new List<CreatureGenotypeEval>();
            if (initialGenotype == null)
            {
                // Random generation
                for (int i = 0; i < size; i++)
                {
                    CreatureGenotypeEval cgEval = new CreatureGenotypeEval(MutateGenotype.GenerateRandomCreatureGenotype(mp));
                    g.cgEvals.Add(cgEval);
                }
            }
            else
            {
                // Mutated generation
                int parentCount = 10;
                for (int i = 0; i < parentCount; i++)
                {
                    CreatureGenotypeEval cgEval = new CreatureGenotypeEval(initialGenotype.Clone());
                    g.cgEvals.Add(cgEval);
                }
                for (int i = 0; i < size- parentCount; i++)
                {
                    // CreatureGenotypeEval cgEval = new CreatureGenotypeEval(initialGenotype);
                    CreatureGenotypeEval cgEval = new CreatureGenotypeEval(MutateGenotype.MutateCreatureGenotype(initialGenotype, mp));
                    g.cgEvals.Add(cgEval);
                }
            }
            return g;
        }

        public static Generation FromMutation(int size, List<CreatureGenotypeEval> topEvals, MutateGenotype.MutationPreferenceSetting mp)
        {
            Generation g = new Generation();

            int topCount = topEvals.Count;
            int remainingCount = size - topEvals.Count;

            List<CreatureGenotypeEval> topSoftmaxEvals = new List<CreatureGenotypeEval>();

            float minFitness = topEvals.Min(x => (float)x.fitness.Value);
            float maxFitness = topEvals.Max(x => (float)x.fitness.Value);
            float scalingFactor = (maxFitness != minFitness) ? 1.0f / (maxFitness - minFitness) : 1.0f;

            // float exponent = 0.5f;
            float temperature = 0.35f; // You can adjust this value to find the right balance
            float denom = topEvals.Select(x => Mathf.Pow((float)(x.fitness.Value - minFitness) * scalingFactor, 1 / temperature)).Sum();
            //float denom = topEvals.Select(x => Mathf.Exp((float)x.fitness.Value - maxFitness)).Sum();
            topEvals = topEvals.OrderByDescending(x => x.fitness.Value).ToList();

            //Debug.Log(string.Format("[{0}, {1}], scaling factor {2}, denom {3}", maxFitness, minFitness, scalingFactor, denom));
            foreach (CreatureGenotypeEval topEval in topEvals)
            {
                g.cgEvals.Add(new CreatureGenotypeEval(topEval.cg));
                CreatureGenotypeEval topSoftmaxEval = topEval.ShallowCopy();
                //topSoftmaxEval.fitness = Mathf.Exp((float)topSoftmaxEval.fitness.Value - maxFitness);
                //topSoftmaxEval.fitness = Mathf.Pow((float)topSoftmaxEval.fitness.Value, exponent);
                float fitnessProbability = Mathf.Pow((float)(topSoftmaxEval.fitness.Value - minFitness) * scalingFactor, 1 / temperature);
                topSoftmaxEval.fitness = Mathf.Pow((float)(topSoftmaxEval.fitness.Value - minFitness) * scalingFactor, 1 / temperature);
                topSoftmaxEvals.Add(topSoftmaxEval);
            }

            topSoftmaxEvals = topSoftmaxEvals.OrderByDescending(x => x.fitness.Value).ToList();

            List<int> intValues = new List<int>();

            // Calculate the integer values for each percentage
            int sizeChildren = size - topEvals.Count;
            foreach (CreatureGenotypeEval topSoftmaxEval in topSoftmaxEvals)
            {
                intValues.Add((int)(sizeChildren * (float)topSoftmaxEval.fitness.Value / denom));
            }

            // Calculate the difference between the sum of the integer values and the desired size
            int diff = sizeChildren - intValues.Sum();

            // Distribute the difference across the integer values
            for (int i = 0; i < diff; i++)
            {
                intValues[i % intValues.Count]++;
            }

            for (int i = 0; i < intValues.Count; i++)
            {
                CreatureGenotype cg = topSoftmaxEvals[i].cg;
                int childrenCount = intValues[i];
                if (i <= 1) Debug.Log(string.Format("{0} ({1}, {2}), children; {3}/{4}", cg.name, topEvals[i].fitness.Value, topSoftmaxEvals[i].fitness.Value / denom, childrenCount, sizeChildren));
                for (int j = 0; j < childrenCount; j++)
                {
                    g.cgEvals.Add(new CreatureGenotypeEval(MutateGenotype.MutateCreatureGenotype(cg, mp)));
                }
            }

            if (g.cgEvals.Count != size){
                Debug.Log(remainingCount);
                Debug.Log(topSoftmaxEvals.Select(x => x.fitness.Value).ToList());

                foreach (CreatureGenotypeEval topSoftmaxEval in topSoftmaxEvals)
                {
                    Debug.Log(topSoftmaxEval.fitness.Value);
                }

                throw new System.Exception("Wrong generation size!");
            }

            return g;

        }
    }

    public enum EvalStatus { NOT_EVALUATED, EVALUATED, DISQUALIFIED };
    [System.Serializable]
    public class CreatureGenotypeEval {
        public CreatureGenotype cg;
        public SN<float> fitness; // total reward
        public EvalStatus evalStatus = EvalStatus.NOT_EVALUATED;

        public CreatureGenotypeEval(CreatureGenotype cg){
            this.cg = cg;
            fitness = 0;
            evalStatus = EvalStatus.NOT_EVALUATED;
        }

        public CreatureGenotypeEval(CreatureGenotype cg, float fitness)
        {
            this.cg = cg;
            this.fitness = fitness;
            evalStatus = EvalStatus.EVALUATED;
        }

        public CreatureGenotypeEval ShallowCopy(){
            CreatureGenotypeEval cgEval = new CreatureGenotypeEval(cg);
            cgEval.fitness = fitness;
            cgEval.evalStatus = evalStatus;
            return cgEval;
        }
    }

    [System.Serializable]
    public class EnvTracker {
        public Environment env;
        public int? idx;

        public EnvTracker(Environment env, int? idx){
            this.env = env;
            this.idx = idx;
        }
    }

    public class KSSAlgorithm : TrainingAlgorithm
    {
        public KSSSave saveK;
        private KSSSettings optimizationSettings;

        private int currentGenotypeIndex = 0;
        private int currentGenerationIndex = 0;
        [SerializeField]
        private int untestedRemaining;
        private Generation currentGeneration;
        private List<EnvTracker> trackers;
        private bool isSetup = false;
        //private bool generationInProgress = false;

        public override void Setup(TrainingManager tm)
        {
            base.Setup(tm);
            saveK = (KSSSave)save;
            optimizationSettings = (KSSSettings)saveK.ts.optimizationSettings;


            if (saveK.isNew){
                Debug.Log("New save, first generation...");
                CreateNextGeneration(true);
            } else {
                // setup indexes, TODO
                // CreateNextGeneration(false); probably not this actually
            }

            trackers = new List<EnvTracker>();
            foreach (Environment env in tm.environments)
            {
                trackers.Add(new EnvTracker(env, null));
            }

            isSetup = true;
        }

        // Update is called once per frame
        void Update()
        {
            // TODO: currentGenerationIndex needs a stop condition

            if (!isSetup)
                return;

            if (currentGenotypeIndex < optimizationSettings.populationSize)
            {
                // Haven't spawned all, so loop through genotypes
                bool completed = true;
                for (int i = currentGenotypeIndex; i < optimizationSettings.populationSize; i++)
                {
                    CreatureGenotypeEval currentEval = currentGeneration.cgEvals[i];
                    EnvTracker envTracker = FindAvailableEnvironment();
                    if (envTracker != null)
                    {
                        envTracker.idx = i; // Storing genotype index
                        envTracker.env.StartEnv(currentEval.cg);
                        envTracker.env.PingReset(this);
                    } else {
                        // All envs are full, set currentGenotypeIndex and wait until next Update() call
                        currentGenotypeIndex = i;
                        completed = false;
                        break;
                    }
                }

                if (completed){
                    currentGenotypeIndex = optimizationSettings.populationSize;
                }
            }
            else if (untestedRemaining <= 0)
            {
                CreateNextGeneration(false);
            }

            // Update stats text
            tm.statsText.text = string.Format("Gen: {0}, Creatures Remaining: {1}", (currentGenerationIndex + 1).ToString(), untestedRemaining.ToString());
        }

        public override void ResetPing(Environment env, float fitness, bool isDQ)
        {
            // Set fitness info
            EnvTracker result = trackers.First(t => t.env == env);
            if (result == null || result.idx == null){
                throw new System.Exception();
            }
            CreatureGenotypeEval eval = currentGeneration.cgEvals[(int)result.idx];
            eval.fitness = isDQ ? null : fitness;
            eval.evalStatus = isDQ ? EvalStatus.DISQUALIFIED : EvalStatus.EVALUATED;
            untestedRemaining--;
        }

        private EnvTracker FindAvailableEnvironment()
        {
            foreach (EnvTracker et in trackers)
            {
                if (!et.env.busy)
                {
                    return et;
                }
            }
            return null;
        }

        private void CreateNextGeneration(bool first)
        {
            currentGenotypeIndex = 0;

            untestedRemaining = optimizationSettings.populationSize;
            if (first) {
                currentGenerationIndex = 0;
                currentGeneration = Generation.FromInitial(optimizationSettings.populationSize, optimizationSettings.initialGenotype, optimizationSettings.mp);
                saveK.generations = new List<Generation>();
                saveK.generations.Add(currentGeneration);
            } else {
                currentGeneration.Complete();

                currentGenerationIndex++;
                List<CreatureGenotypeEval> topEvals = SelectTopEvals(currentGeneration, optimizationSettings.mp);
                currentGeneration = Generation.FromMutation(optimizationSettings.populationSize, topEvals, optimizationSettings.mp);
                //CreatureGenotype bestGenotype = SelectBestGenotype(currentGeneration);
                //currentGeneration = Generation.FromInitial(optimizationSettings.populationSize, bestGenotype, optimizationSettings.mp);
                saveK.generations.Add(currentGeneration);

                // Delete last generation (TODO: turn this into a top 60 preserving + median + worst
                if (saveK.generations.Count != 2){
                    //CreatureGenotypeEval bestEval = saveK.generations[saveK.generations.Count - 2].SelectBestEval();
                    //saveK.generations[saveK.generations.Count - 2].cgEvals = new List<CreatureGenotypeEval>() { bestEval } ;
                    saveK.generations[saveK.generations.Count - 2].Cull(optimizationSettings.populationSize, optimizationSettings.survivalRatio);
                }
            }
        }

        private List<CreatureGenotypeEval> SelectTopEvals(Generation g, MutateGenotype.MutationPreferenceSetting mp)
        {
            List<CreatureGenotypeEval> cleanedEvals = new List<CreatureGenotypeEval>(g.cgEvals);
            List<CreatureGenotypeEval> topEvals = new List<CreatureGenotypeEval>();
            cleanedEvals.RemoveAll(x => x.evalStatus == EvalStatus.DISQUALIFIED);
            cleanedEvals.RemoveAll(x => x.fitness.HasValue == false);
            List<CreatureGenotypeEval> sortedEvals = cleanedEvals.OrderByDescending(x => x.fitness.Value).ToList();

            int topCount = Mathf.RoundToInt(optimizationSettings.populationSize * optimizationSettings.survivalRatio);
            int positiveCount = 0;
            // topCount is sometimes > size of list so to avoid this, added fix.
            int loopCount = Mathf.Min(topCount, sortedEvals.Count);
            for (int i = 0; i < loopCount; i++)
            {
                if (sortedEvals[i] == null)
                {
                    break;
                }
                CreatureGenotypeEval eval = sortedEvals[i];
                // TEMPORARY FIX: Setting lower boudn to -8 instead of 0. TODO lol
                if (eval.evalStatus == EvalStatus.EVALUATED && eval.fitness != null && eval.fitness.Value >= -8) {
                    topEvals.Add(eval);
                    positiveCount++;
                } else {
                    CreatureGenotypeEval cgEval = new CreatureGenotypeEval(optimizationSettings.initialGenotype.Clone());
                    //CreatureGenotypeEval cgEval = new CreatureGenotypeEval(MutateGenotype.GenerateRandomCreatureGenotype());
                    topEvals.Add(cgEval);
                }
            }

            Debug.Log(string.Format("{0}/{1} Creatures with >=0 fitness.", positiveCount, topCount));
            CreatureGenotypeEval bestEval = GetBestCreatureEval();
            Debug.Log("Best: " + topEvals.Max(x => x.fitness.Value));
            saveK.best = bestEval;

            //bestEval.cg.SaveData("BestCreatures/" + "Gen-" + currentGenerationIndex + " reward= " + bestEval.fitness.Value + ".creature",
            //    true, true); ;

            string path = Path.Combine(OptionsPersist.instance.VCSaves, save.saveName + ".save");
            save.SaveData(path, true, true);
            //saveK.SaveData("C:\\Users\\ajwm8\\Documents\\Programming\\Unity\\UTMIST Virtual Creatures\\Creatures\\longtest\\MAINSAVE.save", true);
            return topEvals;
        }

        public CreatureGenotype GetBestCreatureGenotype(){
            foreach (Generation generation in saveK.generations)
            {
                
            }
            List<CreatureGenotypeEval> topEvals = SelectTopEvals(currentGeneration, optimizationSettings.mp);

            return topEvals[0].cg;
        }

        public CreatureGenotypeEval GetBestCreatureEval(){
            CreatureGenotypeEval bestEval = new CreatureGenotypeEval(null, -999f);
            foreach (Generation generation in saveK.generations)
            {
                if (generation.cgEvals == null) continue;
                foreach (CreatureGenotypeEval cgEval in generation.cgEvals)
                {
                    if (cgEval.evalStatus == EvalStatus.EVALUATED && cgEval.fitness > bestEval.fitness)
                    {
                        bestEval = cgEval;
                    }
                }
            }

            return bestEval;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(KSSAlgorithm))]
    public class KSSAlgorithmEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            KSSAlgorithm alg = target as KSSAlgorithm;

            if (GUILayout.Button("Save Current KSSSave"))
            {
                Debug.Log("Saving Current KSSSave");
                KSSSave save = alg.saveK;
                save.SaveData("/" + save.saveName + ".saveK", false, true);
                Debug.Log(Application.persistentDataPath);
            }

            if (GUILayout.Button("Save Best Creature"))
            {
                Debug.Log("Saving Best Creature");
                CreatureGenotype cg = alg.GetBestCreatureGenotype();
                string path = EditorUtility.SaveFilePanel("Save Creature As", "C:", cg.name + ".creature", "creature");
                cg.SaveData(path, true, true);
                Debug.Log(Application.persistentDataPath);
            }
        }
    }
#endif

}