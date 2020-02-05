namespace DAIHard.Contracts.Query.ContractDefinition

open System
open System.Threading.Tasks
open System.Collections.Generic
open System.Numerics
open Nethereum.Hex.HexTypes
open Nethereum.ABI.FunctionEncoding.Attributes
open Nethereum.Web3
open Nethereum.RPC.Eth.DTOs
open Nethereum.Contracts.CQS
open Nethereum.Contracts
open System.Threading

    
    
    type QueryDeployment(byteCode: string) =
        inherit ContractDeploymentMessage(byteCode)
        
        static let BYTECODE = "0x608060405234801561001057600080fd5b506104fe806100206000396000f3fe608060405234801561001057600080fd5b5060043610610048576000357c01000000000000000000000000000000000000000000000000000000009004806306894f8d1461004d575b600080fd5b6100af6004803603604081101561006357600080fd5b81019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff1690602001909291905050506100ee565b604051808261057860200280838360005b838110156100db5780820151818401526020810190506100c0565b5050505090500191505060405180910390f35b6100f66104a5565b6100fe6104a5565b600080905060008090505b8573ffffffffffffffffffffffffffffffffffffffff16634f127aae6040518163ffffffff167c010000000000000000000000000000000000000000000000000000000002815260040160206040518083038186803b15801561016b57600080fd5b505afa15801561017f573d6000803e3d6000fd5b505050506040513d602081101561019557600080fd5b8101908080519060200190929190505050811015610411576000808773ffffffffffffffffffffffffffffffffffffffff1663f838b82584896040518363ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401808381526020018273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200192505050604080518083038186803b15801561025057600080fd5b505afa158015610264573d6000803e3d6000fd5b505050506040513d604081101561027a57600080fd5b810190808051906020019092919080519060200190929190505050915091506000821180156102aa575060008114155b156103f4576103af8873ffffffffffffffffffffffffffffffffffffffff166342f2b6ec858a6040518363ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401808381526020018273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019250505060206040518083038186803b15801561035357600080fd5b505afa158015610367573d6000803e3d6000fd5b505050506040513d602081101561037d57600080fd5b8101908080519060200190929190505050866000610578811061039c57fe5b602002015161041d90919063ffffffff16565b85600061057881106103bd57fe5b6020020181815250506103da60018561041d90919063ffffffff16565b935082858561057881106103ea57fe5b6020020181815250505b505061040a60018261041d90919063ffffffff16565b9050610109565b50819250505092915050565b60008082840190508381101561049b576040517f08c379a00000000000000000000000000000000000000000000000000000000081526004018080602001828103825260108152602001807f75696e74323536206f766572666c6f770000000000000000000000000000000081525060200191505060405180910390fd5b8091505092915050565b6040518061af0001604052806105789060208202803883398082019150509050509056fea265627a7a7231582083c45532862660ce799abc7690b5ac1b79e7f233b92262ac96d29a80b954bdb564736f6c63430005100032"
        
        new() = QueryDeployment(BYTECODE)
        

        
    
    [<Function("getExitInfo", "uint256[1400]")>]
    type GetExitInfoFunction() = 
        inherit FunctionMessage()
    
            [<Parameter("address", "_bucketSale", 1)>]
            member val public BucketSale = Unchecked.defaultof<string> with get, set
            [<Parameter("address", "_buyer", 2)>]
            member val public Buyer = Unchecked.defaultof<string> with get, set
        
    
    [<FunctionOutput>]
    type GetExitInfoOutputDTO() =
        inherit FunctionOuputDTO() 
            [<Parameter("uint256[1400]", "", 1)>]
            member val public ReturnValue1 = Unchecked.defaultof<List<BigInteger>> with get, set
    

