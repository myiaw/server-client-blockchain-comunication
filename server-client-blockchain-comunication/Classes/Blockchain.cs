using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace server_client_blockchain_communication.Classes;

[Serializable]
public class Blockchain{
    public const int TARGET_BLOCK_TIME = 5; // seconds
    public const int DIFF_ADJUST_INTERVAL = 5; // blocks
    private static List<int> peers = new();
    public List<Block> _chain;
    public ObservableCollection<Block> _chainUI;

    public Blockchain() {
        var genesisBlock = new Block(0, "Genesis Block", DateTime.UtcNow, null, 0, 1);
        genesisBlock.Hash = genesisBlock.CalculateHash();
        while (!genesisBlock.IsHashValid()) {
            genesisBlock.Nonce++;
            genesisBlock.Hash = genesisBlock.CalculateHash();
        }

        _chain = new List<Block> { genesisBlock };
        _chainUI = new ObservableCollection<Block> { genesisBlock };
    }


    public List<Block> GetChain() {
        return _chain;
    }

    public int getBlockTime() {
        return TARGET_BLOCK_TIME;
    }

    public int getAdjustInterval() {
        return DIFF_ADJUST_INTERVAL;
    }

    private Block GetLatestBlock() {
        return _chain.LastOrDefault();
    }


    public void AddToChain(Block block) {
        if (block.ValidateBlock()) {
            _chain.Add(block);
            _chainUI.Add(block);
        }
        else {
            _chainUI.Add(block);
        }
    }

    public Block Mine() {
        if (GetLatestBlock() == null) return null;
        var block = new Block(GetLatestBlock().Index + 1, "Block " + (GetLatestBlock().Index + 1),
            DateTime.UtcNow,
            GetLatestBlock().Hash, 0, GetLatestBlock().Difficulty);
        block.Hash = block.CalculateHash();
        block.Difficulty = CalculateDifficulty(DIFF_ADJUST_INTERVAL, TARGET_BLOCK_TIME);

        while (!block.IsHashValid()) {
            block.Nonce++;
            block.Hash = block.CalculateHash();
        }

        return block;
    }


    public static bool ValidateChain(List<Block> list) {
        if (list[0].Data != "Genesis Block" || list[0].PreviousHash != null) return false;
        for (var i = 0; i < list.Count; i++) {
            var block = list[i];
            //Check if values are set correctly.
            if (block.PreviousHash != list[i - 1].Hash) return false;
            //Check if indexes are in order.
            if (block.Index - 1 != list[i - 1].Index) return false;
            //Validate each block.
            if (!block.ValidateBlock()) return false;
        }
        return true;
    }

    public int CalculateDifficulty(int blockGenerationInterval, int diffAdjustInterval) {
        if (_chain.Count < diffAdjustInterval) return 1;
        var previousAdjustmentBlock = _chain[_chain.Count - diffAdjustInterval];
        var timeExpected = blockGenerationInterval * diffAdjustInterval;
        var timeTaken = _chain.Last().Timestamp - previousAdjustmentBlock.Timestamp;
        if (timeTaken.TotalMinutes < timeExpected / 2) return previousAdjustmentBlock.Difficulty + 1;
        if (timeTaken.TotalMinutes > timeExpected * 2) return previousAdjustmentBlock.Difficulty - 1;
        return previousAdjustmentBlock.Difficulty;
    }
}