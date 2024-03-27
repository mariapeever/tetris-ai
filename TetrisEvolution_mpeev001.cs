using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AlanZucconi.AI.Evo;
using System.Linq;
using System.IO;
using UnityEditor;
using System;

public class TetrisEvolution_mpeev001 : EvolutionSystem<ArrayGenome>
{
    [Space(100)]
    
    public string FirstGenomePath = "(Default)";
    public bool EnterManually = false;

    public bool LoadWeights;
    public string WeightsDir = "(Default)";
    public string LogPath = "(Default)";

    private int Heuristics = 6;
    private int Parts = 4;
    private int PrevGenerations;
    private void Start()
    {
        FirstGenomePath = $"{Application.dataPath}/Tetris/AIs/Students/2021-22/mpeev001/FirstGenome.csv";
        if (WeightsDir.Equals("(Default)")) WeightsDir = $"{Application.dataPath}/Tetris/AIs/Students/2021-22/mpeev001/Weights";
        if (LogPath.Equals("(Default)")) LogPath = $"{Application.dataPath}/Tetris/AIs/Students/2021-22/mpeev001/Log.csv";
        PrevGenerations = (LoadWeights && File.Exists(LogPath)) ? File.ReadLines(LogPath).Count() - 1 : 0;
    }

    [Button]
    public new void StartEvolution()
    {
        StartCoroutine(StartEvolutionCoroutine());
    }

    IEnumerator StartEvolutionCoroutine()
    {
        // ======================
        // === INITIALISATION ===
        // ======================

        // FindObjectsOfType cannot retrieve interfaces
        // So we get all monobehaviours and filter for World<ArrayGenome>
        Worlds = FindObjectsOfType<MonoBehaviour>()
            .OfType<IWorld<ArrayGenome>>()
            .ToList();
        
        //Worlds = FindObjectsOfType<World<ArrayGenome>>()

        // Uses the first GenomeFactory<ArrayGenome> to instantiate the genomes
        Factory = FindObjectsOfType<MonoBehaviour>()
            .OfType<IGenomeFactory<ArrayGenome>>()
            .First();
        //Factory = Worlds[0] as GenomeFactory<ArrayGenome>; // Uses the first one as a factory

        // Waits one frame to make sure Awake() and Start() have been called
        yield return null;


        // ========================
        // === FIRST POPULATION ===
        // ========================

        if (AddFirstGenome && !EnterManually)
        {
            int H = Heuristics;
            int Params = (int)(Mathf.Pow(Parts, H) * H);
            FirstGenome = LoadGenome(FirstGenomePath, Params);
        }

        if (AddFirstGenome)
        {
            // Adds the first genome
            Population.Add((ArrayGenome)FirstGenome.Copy());
            // Adds mutations of the first genome
            for (int i = 0 + 1; i < Worlds.Count; i++)
            {
                ArrayGenome genome = (ArrayGenome)FirstGenome.Copy();
                int mutations = UnityEngine.Random.Range(0, Mutations);
                for (int m = 0; m < mutations; m++)
                    genome.Mutate();

                Population.Add(genome);
            }
        }
        else if (LoadWeights)
        {
            //base.AddFirstGenome = true;
            int H = Heuristics;
            int Params = (int)(Mathf.Pow(Parts, H) * H);
            foreach (string CSVPath in Directory.EnumerateFiles(WeightsDir, "*.csv"))
            {
                int Id = int.Parse(CSVPath.Split('/').Last().Split('-').Last().Split('.').First());
                Population.Add(LoadGenome(CSVPath, Params, Id));

            }

            float[] PrevScores = File.ReadAllLines(LogPath).Select(ln => float.Parse(ln.Split(' ')[1])).ToArray();

            for (int i = 0; i < PrevScores.Length; i++) { 
                PlotData.Add
                (
                    new Vector2(i, PrevScores[i])
                );
            }
            

        }
        else
        {
            // Initialises the random population
            for (int i = Population.Count(); i < Worlds.Count; i++)
            {
                ArrayGenome genome = (ArrayGenome)Factory.Instantiate();
                Population.Add(genome);
            }
        }


        // ======================
        // === EVOLUTION LOOP ===
        // ======================

        // Loops through the generations
        for (int generation = PrevGenerations; generation < Generations; generation++)
        {
            Debug.Log("Generation: " + (generation + 1));

            // -----------------------------------------
            // [TESTS]
            // Tests each world a numer of times
            // to make sure the score are reliable

            // Associates a list of scores to each world
            Dictionary<IWorld<ArrayGenome>, List<float>> scores = new Dictionary<IWorld<ArrayGenome>, List<float>>();
            foreach (IWorld<ArrayGenome> world in Worlds)
                scores.Add(world, new List<float>());

            // Loops through all the necessary tests per genome
            for (int test = 0; test < TestsPerGenome; test++)
            {
                Debug.Log("\tTest: " + (test + 1));

                // [SETUP]
                // Initialises the worlds
                //foreach (World<ArrayGenome> world in Worlds)
                for (int i = 0; i < Worlds.Count; i++)
                {
                    IWorld<ArrayGenome> world = Worlds[i];
                    ArrayGenome genome = Population[i];

                    world.ResetSimulation();
                    world.SetGenome(genome);
                    world.StartSimulation();
                }


                // -----------------------------------------
                // [SIMULATION]
                // Waits for all worlds to be done
                yield return new WaitUntil
                (
                    () => Worlds.All(world => world.IsDone())
                );

                
                // ----------------------------------------
                // Adds the score to the scores list
                foreach (IWorld<ArrayGenome> world in Worlds)
                    scores[world].Add(world.GetScore());
                
                    
            }
            // -----------------------------------------
            // [FITNESS]
            // Gets the top genomes
            List<ArrayGenome> topGenomes = Worlds
                .Rank(world => scores[world].Average(), (int)(Worlds.Count * SurvivalRate))
                .Select(world => world.GetGenome())
                .ToList();

            //foreach (IWorld<ArrayGenome> world in Worlds)
            //    Debug.Log(world + "\t" + scores[world].Count() + "\t" + scores[world].Average());

            /*
            List<ArrayGenome> topGenomes = Worlds
                .Rank(world => world.GetScore(), (int)(Worlds.Count * SurvivalRate))
                .Select(world => world.GetGenome())
                .ToList();
            */
            // Updates scores
            float maxScore = Worlds
                .Select(world => scores[world].Average())
                .Max();

            PlotData.Add
            (
                new Vector2(generation, maxScore)
            );
            Debug.Log("\t=> Best score: " + maxScore);
            Debug.Log("\t=> Best genome: " + topGenomes[0]);

            // Log data
            string Log = $"{generation} {maxScore}" ;
            foreach(float Param in topGenomes[0].Params)
            {
                Log += " " + Param;
            }
            Log += Environment.NewLine;
            File.AppendAllText(LogPath, Log);

            // Mutations from the top genomes
            Population.Clear();
            // Adds the best one back
            Population.Add((ArrayGenome)topGenomes[0].Copy());
            SaveWeights(0);

            // Adds the remaining ones and mutates them
            for (int i = 0 + 1; i < Worlds.Count; i++)
            {
                ArrayGenome genome = (ArrayGenome)topGenomes[UnityEngine.Random.Range(0, topGenomes.Count)].Copy();
                int mutations = UnityEngine.Random.Range(0, Mutations);
                for (int m = 0; m < mutations; m++)
                    genome.Mutate();

                Population.Add(genome);
                SaveWeights(i);
            }


            // Waits next frame before restarting
            yield return null;
        }
    }

    private ArrayGenome LoadGenome(string CSVPath, int Params, int WorldIdx = -1)
    {
        try
        {
            if (!File.Exists(CSVPath))
            {
                string GenomeName = (WorldIdx == -1) ? "First Genome" : $"World {WorldIdx} Genome";
                FileNotFoundException e = new FileNotFoundException($"Weights for {GenomeName} not found: {CSVPath}.");
                throw e;
            }

            ArrayGenome Genome = new ArrayGenome(Params);

            int Index = 0;
            using (var Reader = new StreamReader(CSVPath))
            {
                while (!Reader.EndOfStream)
                {
                    string Line = Reader.ReadLine();
                    Genome.Params[Index++] = float.Parse(Line);
                }
            }

            return Genome;
        }
        catch (FileNotFoundException e)
        {
            this.LoadWeights = false;
            EditorUtility.DisplayDialog("Weights Not Found.", e.Message, "Ok");
        }
        return new ArrayGenome();
    }

    void SaveWeights(int id)
    {
      
        float[] Genome = this.Population[id].Params;

        String CSV = String.Join(
            Environment.NewLine,
            Genome
        );

        string WeightsDir = this.WeightsDir;
        if (!WeightsDir.Last().Equals("/")) WeightsDir += "/";

        string CSVPath = $"{WeightsDir}Weights-{id}.csv";
        if (File.Exists(CSVPath))
            File.Delete(CSVPath);
        File.WriteAllText(CSVPath, CSV);
    }

}

