using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class DFParameters<Gb, M, T> where Gb : PokemonGame
                                    where M : Map<M, T>
                                    where T : Tile<M, T> {

    public bool PruneAlreadySeenStates = true;
    public int MaxCost = 0;
    public int SuccessSS = 1;
    public int RNGSS = -1;
    public string LogStart = "";
    public T[] EndTiles = null;
    public int EndEdgeSet = 0;
    public Func<Gb, bool> EncounterCallback = null;
    public Action<DFState<M, T>> FoundCallback;
}

public class DFState<M, T> where M : Map<M, T>
                           where T : Tile<M, T> {

    public T Tile;
    public int EdgeSet;
    public int WastedFrames;
    public Action BlockedActions;
    public Action LastDir;
    public int APressCounter;
    public IGTResults IGT;
    public string Log;

    public override int GetHashCode() {
        unchecked {
            const int prime = 92821;
            int hash = prime + Tile.Map.Id;
            hash = hash * prime + Tile.X;
            hash = hash * prime + Tile.Y;
            hash = hash * prime + IGT.MostCommonHRA;
            hash = hash * prime + IGT.MostCommonHRS;
            hash = hash * prime + IGT.MostCommonDivider;
            hash = hash * prime + IGT.TotalSuccesses;
            hash = hash * prime + IGT.TotalOverworld;
            return hash;
        }
    }
}

public static class DepthFirstSearch {

    public static void StartSearch<Gb, M, T>(Gb[] gbs, DFParameters<Gb, M, T> parameters, T startTile, int startEdgeSet, IGTResults initialState, int initAPress = 1) where Gb : PokemonGame
                                                                                                                                                                      where M : Map<M, T>
                                                                                                                                                                      where T : Tile<M, T> {
        if(parameters.FoundCallback == null)
            parameters.FoundCallback = state => Console.WriteLine(state.Log);
        wildEncounterAddress = gbs[0].SYM["CalcStats"];

        RecursiveSearch(gbs, parameters, new DFState<M, T> {
            Tile = startTile,
            EdgeSet = startEdgeSet,
            WastedFrames = 0,
            Log = parameters.LogStart,
            APressCounter = initAPress,
            IGT = initialState,
        }, new HashSet<int>());
    }

    static int wildEncounterAddress;

    private static void RecursiveSearch<Gb, M, T>(Gb[] gbs, DFParameters<Gb, M, T> parameters, DFState<M, T> state, HashSet<int> seenStates) where Gb : PokemonGame
                                                                                                                                             where M : Map<M, T>
                                                                                                                                             where T : Tile<M, T> {

        // if(state.Log.StartsWith("UUAUUUUUUUUUUUUUUUUURUUUAUU"))
        //     Console.WriteLine(state.Log+" hash:"+state.GetHashCode()+" "+state.Tile.X+" "+state.Tile.Y+" "+state.Tile.Collision+" "+state.IGT.MostCommonHRA+" "+state.IGT.MostCommonHRS+" "+state.IGT.MostCommonDivider+" "+state.IGT.TotalSuccesses);

        if(parameters.EndTiles != null && state.EdgeSet == parameters.EndEdgeSet && parameters.EndTiles.Any(t => t.X == state.Tile.X && t.Y == state.Tile.Y)) {
            if(parameters.EncounterCallback == null) {
                parameters.FoundCallback(state);
            }
            return;
        }

        if(parameters.PruneAlreadySeenStates && !seenStates.Add(state.GetHashCode())) {
            return;
        }

        foreach(Edge<M, T> edge in state.Tile.Edges[state.EdgeSet].OrderBy(x => x.Action != state.LastDir)) { // always try the same direction first
            if(state.WastedFrames + edge.Cost > parameters.MaxCost) continue;
            if((state.BlockedActions & edge.Action) > 0) continue;
            if(edge.Action == Action.A && state.APressCounter > 0) continue;

            // IGTResults results = PokemonGame.IGTCheckParallel<Gb>(gbs, state.IGT, gb => gb.Execute(edge.Action) == gb.OverworldLoopAddress, parameters.NoEncounterSS);
            IGTResults results = new IGTResults(state.IGT.Length);
            MultiThread.For(state.IGT.Length, gbs, (gb, f) => {
                IGTState prev=state.IGT[f];
                IGTState igt;
                if(prev.State != null)
                {
                    gb.LoadState(prev.State);
                    int ret = gb.Execute(edge.Action);
                    if(ret == gb.OverworldLoopAddress)
                        igt = new IGTState(gb, prev.Success, prev.IGTStamp);
                    else
                    {
                        igt = new IGTState();
                        igt.IGTStamp=prev.IGTStamp;
                        if(ret == wildEncounterAddress) {
                            igt.Success = parameters.EncounterCallback(gb);
                        } else {
                            Console.WriteLine("Movement failed on frame " + f);
                        }
                    }
                }
                else
                {
                    igt = prev; // copy previous state
                }
                results[f]=igt;
            });

            DFState<M, T> newState = new DFState<M, T>() {
                Tile = edge.NextTile,
                EdgeSet = edge.NextEdgeset,
                Log = state.Log + edge.Action.LogString(),
                IGT = results,
                WastedFrames = state.WastedFrames + edge.Cost,
            };

            int totalSuccesses=results.TotalSuccesses;
            int totalOverworld=results.TotalOverworld;

            if(totalSuccesses >= parameters.SuccessSS) { // success
                parameters.FoundCallback(newState);
            }

            if(totalOverworld > 0 && totalOverworld + totalSuccesses >= parameters.SuccessSS) { // success still possible
                int rngSuccesses = results.RNGSuccesses(0x9);
                if(rngSuccesses >= parameters.RNGSS) {
                    newState.APressCounter = edge.Action == Action.A ? 2 : Math.Max(state.APressCounter - 1, 0);

                    Action blockedActions = state.BlockedActions;
                    if(edge.Action == Action.A) blockedActions |= Action.StartB;
                    else blockedActions &= ~(Action.A | Action.StartB);
                    newState.BlockedActions = blockedActions;

                    if((edge.Action & (Action.Up | Action.Down | Action.Left | Action.Right)) != 0)
                        newState.LastDir = edge.Action;
                    else
                        newState.LastDir = state.LastDir;

                    RecursiveSearch(gbs, parameters, newState, seenStates);
                }
            }
        }
    }
}