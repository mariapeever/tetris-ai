using UnityEngine;using AlanZucconi.Tetris;using System.Linq;
using System.Collections.Generic;
using Isotope.Collections;

using AlanZucconi.AI.Evo;
using System.IO;
using UnityEditor;

[CreateAssetMenu(    fileName = "TetrisAI_mpeev001",    menuName = "Tetris/2021-22/TetrisAI_mpeev001")]public class TetrisAI_mpeev001 : TetrisAI{
   
    public Dictionary<string, float[]> Genome;
    public string WeightsPath = $"{Directory.GetCurrentDirectory()}/Assets/Tetris/AIs/Students/2021-22/mpeev001/Weights/Weights-0.csv";
    public bool SinglePlay = false;

    private float Part = 0.25f;
    private int Parts = 4;
    private int Heuristics = 6;
    private bool LoadWeights = true;
    public override int ChooseMove(Move[] moves)
    {
        /* Uncomment to set the weights manually or
           to run the game separately or using the Automator */
        if (SinglePlay)
        {
            this.Genome = this.BuildTree(this.Instantiate(this.WeightsPath));
        }

        int[] Indexes = Enumerable.Range(0, moves.Length).ToArray();
        BitArray2D board = Tetris.State.Board.Clone();
        float P = this.Part;
        float[] Scores = moves.Select(move => {

            float[] RowFilling = this.SelectRowFilling(move);
            
            float BrokenRows = RowFilling[0];
            float Fullness = RowFilling[1];

            float Fit = this.SelectFit(move);
            float[] BoardState = this.SelectBoardState(move);
            float AvailableSpace = BoardState[0];
            float BlockedSquares = BoardState[1];
            float UnblockedSquares = BoardState[2];
            
            float FullnessKey = this.GenScore(Fullness);
            float BrokenRowsKey = this.GenScore(BrokenRows);
            float FitKey = this.GenScore(Fit);
            float BlockedSquaresKey = this.GenScore(BlockedSquares);
            float AvailableSpaceKey = this.GenScore(AvailableSpace);
            float UnblockedSquaresKey = this.GenScore(UnblockedSquares);
            
            float[] Scores = new float[] { BrokenRows, Fullness, Fit, BlockedSquares, AvailableSpace, UnblockedSquares };
            string Key = BrokenRowsKey + " " + FullnessKey + " " + FitKey + " " + BlockedSquaresKey + " " + AvailableSpaceKey + " " + UnblockedSquaresKey;
            float[] Weights = this.Genome[Key];
            return Enumerable.Range(0, Scores.Length).Sum(i => Scores[i] * Weights[i]);

        }).ToArray();

        //Fiter moves by fit score
        float MaxScore = Scores.Max();
        int[] MaxScores = Indexes.Where((index, i) => Scores[i] == MaxScore).ToArray();
        int idx = Random.Range(0, MaxScores.Length);
        return MaxScores[idx];

    }    private float GenScore(float Score)
    {
        float P = this.Part;
        return Mathf.Min(Mathf.Abs(Mathf.Floor(Score / P) * P), 1 - P);
    }       private BitArray2D Flood(BitArray2D Board)
    {
        BitArray2D Copy = Board.Clone();
        for (int x = 0; x < Board.Width; x++)
        {
            for (int y = Board.Height - 1; y >= 0 ; y--)
            {
                if (Copy.Get(x, y).Equals(true)) break;
                else Board.Set(x, y, true);

                //if (x > 0)
                //{
                //    int l = x - 1;

                //    while (l >= 0 && Board.Get(l, y).Equals(false))
                //    {
                //        Board.Set(l--, y, true);
                //    }
                //}
                //if (x < Board.Width - 1)
                //{
                //    int r = x + 1;
                //    while (r < Board.Width && Board.Get(r, y).Equals(false))
                //    {
                //        Board.Set(r++, y, true);
                //    }
                //}
            }
        }
        return Board;
    }    private int CountEmpty(BitArray2D Board)
    {
        int Empty = 0;
        for (int x = 0; x < Board.Width; x++)
        {
            Empty += Enumerable.Range(0, Board.Height).Count(y => Board.Get(x, y).Equals(true));
        }
        return Empty;
    }

    private int CountFull(BitArray2D Board)
    {
        int Full = 0;
        for (int x = 0; x < Board.Width; x++)
        {
            Full += Enumerable.Range(0, Board.Height).Count(y => Board.Get(x, y).Equals(false));
        }
        return Full;
    }    private float[] SelectBoardState(Move move)
    {
        BitArray2D Init = Tetris.State.Board.Clone();
        BitArray2D Simulated = Tetris.State.SimulateMove(move).Board.Clone();

        BitArray2D InitFlood = this.Flood(Init);
        BitArray2D SimulatedFlood = this.Flood(Simulated);

        int InitCount = this.CountEmpty(InitFlood);

        int SimulatedEmptyCount = this.CountEmpty(SimulatedFlood);
        int SimulatedFullCount = this.CountFull(Simulated);
        int Blocked = -Mathf.Max(SimulatedEmptyCount - InitCount, 0);
        int Unblocked = Mathf.Abs(Mathf.Min(SimulatedEmptyCount - InitCount, 0));
        int Area = Init.Width * Init.Height;
        
        int Space = Area - SimulatedFullCount - Blocked;
        
        // return N Blocked Score and Available Space Score
        return new float[] { ((float) Space / Area), ((float) Blocked / Area), ((float) Unblocked / Area) };
      
    }    private float[] SelectRowFilling(Move move)
    {
        BitArray2D Board = Tetris.State.Board.Clone();

        Tetromino Tetromino = move.Tetromino;
        Vector2Int Pos = move.Position;

        int Start = Pos.y;
        int End = Pos.y + Tetromino.Height;

        int Fullness = 0;
        int BrokenRows = 0;

        for (int y = Start; y < End; y++)
        {
            int BoardRow = Enumerable.Range(0, Board.Width).Count(x => Board.Get(x, y).Equals(true));
            int TetrominoRow = Enumerable.Range(0, Tetromino.Width).Count(x => Tetromino.Area[x, y-Start].Equals(true));
            int RowFullness = BoardRow + TetrominoRow;
            if (RowFullness == Board.Width) BrokenRows++;
            if (RowFullness > Fullness) Fullness = RowFullness;
            
        }
        return new float[] { ((float) BrokenRows / Tetromino.Height), ((float) Fullness / Board.Width) };
    }    private float SelectFit(Move move)
    {

        Vector2Int BoardSize = new Vector2Int(Tetris.State.Board.Width, Tetris.State.Board.Height);
        Vector2Int Pos = move.Position;
        int a = 1;

        Vector2Int Start = new Vector2Int(
            Mathf.Max(Pos.x - a, 0),
            Mathf.Max(Pos.y - a, 0));

        Tetromino Tetromino = move.Tetromino;

        Vector2Int End = new Vector2Int(
            Mathf.Min(Pos.x + Tetromino.Width + a, BoardSize.x),
            Mathf.Min(Pos.y + Tetromino.Height + a, BoardSize.y));
        BitArray2D Board = Tetris.State.Board.Clone();

        bool[,] TetrominoArea = move.Tetromino.Area;
        bool[,] ExpandedArea = new bool[End.x - Start.x, End.y - Start.y];
        int sx = (Pos.x == 0) ? 0 : 1;
        int sy = (Pos.y == 0) ? 0 : 1;
        int ex = sx + Tetromino.Width;
        int ey = sy + Tetromino.Height;
        
        for (int i = sx; i < ex; i++)
        {
            for (int j = sy; j < ey; j++)
            {
                ExpandedArea[i, j] = TetrominoArea[i - sx, j - sy];
            }
        }
        
        int Fit = 0;
        int MaxFit = 0;

        int tx = 0;
        int ty;
        for (int x = Start.x; x < End.x; x++)
        {
            ty = 0;
            for (int y = Start.y; y < End.y; y++)
            {
                bool Mould = false;

                if (ExpandedArea[tx, ty].Equals(true) ||
                    x < End.x - 1 && ExpandedArea[(tx + 1), ty].Equals(true) ||
                    x > Start.x && ExpandedArea[(tx - 1), ty].Equals(true) ||
                    y < End.y - 1 && ExpandedArea[tx, (ty + 1)].Equals(true) ||
                    y > Start.y && ExpandedArea[tx, (ty - 1)].Equals(true)) Mould = true;

                if (Mould && Board.Get(x, y).Equals(true) ||
                    ExpandedArea[tx, ty].Equals(true) && (Pos.y == 0 && ty == 0))
                {
                    Fit++;
                }
                if (Mould && (ExpandedArea[tx, ty].Equals(false) || ExpandedArea[tx, ty].Equals(true) && (Pos.x == 0 && x == 0 || Pos.y == 0 && y == 0))) MaxFit++;
                ty++;
            }
            tx++;
        }

        // return Fit Score
        return ((float) Fit / MaxFit);

    }    public Dictionary<string, float[]> BuildTree(ArrayGenome genome)
    {
        int P = this.Parts;
        int H = this.Heuristics;

        Dictionary<string, float[]> Tree = new Dictionary<string, float[]>();
        float Incr = 1f / P;
        int Index = 0;
        for (int n = 0; n < P; n++)
        {
            float k1 = n * Incr;
            for (int o = 0; o < P; o++)
            {
                float k2 = o * Incr;
                for (int p = 0; p < P; p++)
                {
                    float k3 = p * Incr;
                    for (int q = 0; q < P; q++)
                    {
                        float k4 = q * Incr;
                        for (int r = 0; r < P; r++)
                        {
                            float k5 = r * Incr;
                            for (int s = 0; s < P; s++)
                            {
                                float[] Params = new float[]
                                {
                                    genome.Params[Index++],
                                    genome.Params[Index++],
                                    genome.Params[Index++],
                                    genome.Params[Index++],
                                    genome.Params[Index++],
                                    genome.Params[Index++]
                                };

                                float k6 = s * Incr;
                                string Key = k1 + " " + k2 + " " + k3 + " " + k4 + " " + k5 + " " + k6;
                                Tree.Add(Key, Params);
                            }
                        }
                    }
                }
            }
        }
        return Tree;
    }

    public ArrayGenome Instantiate(string WeightsPath = "")
    {
        // Genome with 3 numbers instantialised randomly
        int P = this.Parts;
        int H = this.Heuristics;
        int Params = (int)(Mathf.Pow(Parts, H) * H);

        ArrayGenome genome = new ArrayGenome(Params);

        if (!WeightsPath.Equals(""))
            return LoadGenome(WeightsPath, Params);

        genome.InitialiseRandom();
        return genome;
    }    private ArrayGenome LoadGenome(string CSVPath, int Params, int WorldIdx = -1)
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
            EditorUtility.DisplayDialog("Weights Not Found.", e.Message, "Ok");
        }
        return new ArrayGenome();
    }}