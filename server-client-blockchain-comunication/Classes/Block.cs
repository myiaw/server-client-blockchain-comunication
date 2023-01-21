using System;
using System.Security.Cryptography;
using System.Text;

namespace server_client_blockchain_communication.Classes;


[Serializable]
public class Block{
    public Block(int index, string data, DateTime timestamp, string previousHash, int nonce, int difficulty) {
        Index = index;
        Data = data;
        Timestamp = timestamp;
        PreviousHash = previousHash;
        Nonce = nonce;
        Difficulty = difficulty;
        Hash = "";
    }

    public int Index { get; set; }
    public DateTime Timestamp { get; set; }
    public string Data { get; set; }
    public string PreviousHash { get; set; }
    public string Hash { get; set; }

    public int Difficulty { get; set; }
    public int Nonce { get; set; }

    // The index of the block in the chain


    // The block is valid if it has valid properties and it's index is greater than 0
    // --> The first block(Index 0), called the Genesis block is the starting point of blockchain.

    public bool ValidateBlock() {
        if (Index <= 0 || string.IsNullOrEmpty(Data) || string.IsNullOrEmpty(Hash) || Difficulty < 0 || Nonce < 0)
            return false;
        if (CalculateHash() != Hash) return false;

        // If the block's timestamp is more than 1 minute in the past, return false
        return Timestamp >= DateTime.UtcNow - TimeSpan.FromMinutes(1);
    }

    // Calculates the hash of the block
    public string CalculateHash() {
        using var sha256 = SHA256.Create();
        var calculation = Index + Data + Timestamp + PreviousHash + Difficulty + Nonce;
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(calculation));
        Hash = Convert.ToBase64String(hash);

        return Hash;
    }

    public bool IsHashValid() {
        var startZeros = new string('0', Difficulty);
        return Hash.StartsWith(startZeros);
    }

    public override string ToString() {
        return "Data: " + Data + " Timestamp: " + Timestamp + " Hash: " + Hash + " Previous Hash: " + PreviousHash +
               " Nonce: " + Nonce + " Difficulty " + Difficulty;
    }
}