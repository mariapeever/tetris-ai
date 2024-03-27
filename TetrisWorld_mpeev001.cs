using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AlanZucconi.Tetris;
using AlanZucconi.AI.Evo;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEditor;

public class TetrisWorld_mpeev001 : MonoBehaviour,
    IWorld<ArrayGenome>,
    IGenomeFactory<ArrayGenome>
{
    public TetrisGame Tetris;
    public TetrisAI_mpeev001 AI;

    public int Heuristics = 6;
    public int Parts = 4;

    public ArrayGenome Genome;

    public void ResetSimulation() {

        this.AI = ScriptableObject.CreateInstance<TetrisAI_mpeev001>();
        this.Tetris.SetAI(this.AI);
    }

    public ArrayGenome Instantiate()
    {
        // Genome with 3 numbers instantialised randomly
        int P = this.Parts;
        int H = this.Heuristics;
        int Params = (int)(Mathf.Pow(P, H) * H);
        ArrayGenome genome = new ArrayGenome(Params);
        genome.InitialiseRandom();

        return genome;
    }

    public void SetGenome(ArrayGenome genome)
    { 
        this.AI.Genome = this.BuildTree(genome);
        this.Genome = genome;
    }

    public ArrayGenome GetGenome()
    {
        return this.Genome;
    }

    public void StartSimulation() {
        this.Tetris.StartGame();
    }

    public bool IsDone()
    {
        bool IsRunning = this.Tetris.Running;
        if (! IsRunning) this.Tetris.StopGame();
        return ! IsRunning;
    }

    public float GetScore()
    {
        return this.Tetris.Turn;
    }

    public Dictionary<string, float[]> BuildTree(ArrayGenome genome)
    {
        int P = this.Parts;
        int H = this.Heuristics;

        Dictionary<string, float[]> Tree = new Dictionary<string, float[]>();
        float Incr = (float) 1f / P;
        int Index = 0;
        for (int n = 0; n < P; n++)
        {
            float k1 = (float) n * Incr;
            for (int o = 0; o < P; o++)
            {
                float k2 = (float) o * Incr;
                for (int p = 0; p < P; p++)
                {
                    float k3 = (float) p * Incr;
                    for (int q = 0; q < P; q++)
                    {
                        float k4 = (float) q * Incr;

                        for (int r = 0; r < P; r++)
                        {
                            float k5 = (float) r * Incr;

                            for (int s = 0; s < P; s++)
                            {
                                float k6 = (float) s * Incr;
                                string Key = k1 + " " + k2 + " " + k3 + " " + k4 + " " + k5 + " " + k6;

                                float[] Params = new float[]
                                {
                                    genome.Params[Index++],
                                    genome.Params[Index++],
                                    genome.Params[Index++],
                                    genome.Params[Index++],
                                    genome.Params[Index++],
                                    genome.Params[Index++]
                                };

                                Tree.Add(Key, Params);
                            }
                        }
                    }
                }
            }
        }
        return Tree;
    }
}
