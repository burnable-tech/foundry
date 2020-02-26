﻿module BucketSaleTestBase

open TestBase
open System.Numerics
open FsUnit.Xunit
open Foundry.Contracts.BucketSale.ContractDefinition
open Constants
open Foundry.Contracts.Debug.ContractDefinition

let DAI =
    let abi = Abi("../../../../build/contracts/TestToken.json")

    let deployTxReceipt =
        ethConn.DeployContractAsync abi
            [| "MCD DAI stable coin"
               "DAI"
               ethConn.Account.Address
               bucketSupply * bucketCount * BigInteger(100UL) |]
        |> runNow

    let result = ContractPlug(ethConn, abi, deployTxReceipt.ContractAddress)
    result.Query "balanceOf" [| ethConn.Account.Address |] |> should equal (bucketSupply * bucketCount * BigInteger(100UL) * BigInteger(1000000000000000000UL))
    result


let FRY =
    let abi = Abi("../../../../build/contracts/TestToken.json")

    let deployTxReceipt =
        ethConn.DeployContractAsync abi
            [| "Foundry logistics token"
               "FRY"
               ethConn.Account.Address
               BigInteger(1000000UL) |]
        |> runNow

    let result = ContractPlug(ethConn, abi, deployTxReceipt.ContractAddress)
    result


let referrerReward referrerAddress amount =
    if referrerAddress = EthAddress.Zero then
        BigInteger.Zero
    else
        ((amount / BigInteger 1000000000000000000UL) + BigInteger 10000UL)
        

let treasury = 
    let abi = Abi("../../../../build/contracts/Forwarder.json")
    
    let deployTxReceipt =
        ethConn.DeployContractAsync abi
            [| ethConn.Account.Address |]
        |> runNow

    ContractPlug(ethConn, abi, deployTxReceipt.ContractAddress)

let bucketSale =
    let abi = Abi("../../../../build/contracts/BucketSale.json")

    let deployTxReceipt =
        ethConn.DeployContractAsync abi
            [| treasury.Address; startOfSale; bucketPeriod; bucketSupply; bucketCount; FRY.Address; DAI.Address |]
        |> runNow

    ContractPlug(ethConn, abi, deployTxReceipt.ContractAddress)


let addFryMinter newMinter =
    let isMinter = FRY.Query "isMinter" [| newMinter |]
    if not isMinter then
        let addFryMinterTxReceipt =  FRY.ExecuteFunction "addMinter" [| newMinter |]
        addFryMinterTxReceipt |> shouldSucceed
    else
        ()

    FRY.Query "isMinter" [| newMinter |] |> should equal true

let renounceFryMinter minter =
    let renounceFryMinterTxReceipt =  FRY.ExecuteFunction "renounceMinter" [| minter |]
    renounceFryMinterTxReceipt |> shouldSucceed

let seedWithDAI (recipient:string) (amount:BigInteger) =
    let balanceBefore = DAI.Query "balanceOf" [| recipient |] 
    let transferDaiTxReceipt = DAI.ExecuteFunction "transfer" [| recipient; amount |]
    transferDaiTxReceipt |> shouldSucceed
    DAI.Query "balanceOf" [| recipient |] |> should equal (balanceBefore + amount)

let enterBucket sender buyer bucketToEnter valueToEnter referrer =
    valueToEnter |> should greaterThan BigInteger.Zero
    seedWithDAI debug.ContractPlug.Address valueToEnter
    let approveDaiTxReceipt = DAI.ExecuteFunctionFrom "approve" [| bucketSale.Address; valueToEnter |] debug
    approveDaiTxReceipt |> shouldSucceed

    let referrerReferredTotalBefore = bucketSale.Query "referredTotal" [| referrer |]
    let referrerRewardPercBefore = bucketSale.Query "referrerReferralRewardPerc" [| referrer |]
    let calculatedReferrerRewardPercBefore = referrerReward referrer referrerReferredTotalBefore
    referrerRewardPercBefore |> should equal calculatedReferrerRewardPercBefore
    let senderDaiBalanceBefore = DAI.Query "balanceOf" [| debug.ContractPlug.Address |]
    let treasuryDaiBalanceBefore = DAI.Query "balanceOf" [| treasury.Address |]
    let buyForBucketBefore = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketToEnter; buyer |]
    let buyerRewardBuyBefore = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketToEnter + BigInteger.One; buyer |]
    let referrerRewardBuyBefore = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketToEnter + BigInteger.One; referrer |]
    let bucketBefore = bucketSale.QueryObj<BucketsOutputDTO> "buckets" [| bucketToEnter |]
    let referralBucketBefore = bucketSale.QueryObj<BucketsOutputDTO> "buckets" [| bucketToEnter + BigInteger.One |]

    // act
    let receipt = bucketSale.ExecuteFunctionFrom "enter" [| buyer; bucketToEnter; valueToEnter; referrer |] debug

    // assert
    let forwardedEvent = decodeFirstEvent<ForwardedEventDTO> receipt
    let revertMessage = forwardedEvent.ResultAsRevertMessage
    forwardedEvent.Success |> should equal true
    receipt |> shouldSucceed

    // event validation
    let referredTotalAfter = bucketSale.Query "referredTotal" [| referrer |]
    referredTotalAfter |> should equal (referrerReferredTotalBefore + valueToEnter)
    let calculatedReferrerRewardPercAfter = referrerReward referrer referredTotalAfter
    let referrerRewardPercAfter = bucketSale.Query "referrerReferralRewardPerc" [| referrer |]
    referrerRewardPercAfter |> should equal calculatedReferrerRewardPercAfter
    let buyerReward = valueToEnter / BigInteger 10UL
    let referrerReward = valueToEnter * calculatedReferrerRewardPercAfter / BigInteger 100000

    let enteredEvent = receipt |> decodeFirstEvent<EnteredEventDTO>
    if referrer <> EthAddress.Zero then
        enteredEvent.BucketId |> should equal bucketToEnter
        enteredEvent.Buyer |> shouldEqualIgnoringCase buyer
        enteredEvent.BuyerReferralReward |> should equal buyerReward
        enteredEvent.Sender |> shouldEqualIgnoringCase debug.ContractPlug.Address
        enteredEvent.Referrer |> shouldEqualIgnoringCase referrer
        enteredEvent.ReferrerReferralReward |> should equal referrerReward
        enteredEvent.ValueEntered |> should equal valueToEnter
    else
        enteredEvent.BucketId |> should equal bucketToEnter
        enteredEvent.Buyer |> shouldEqualIgnoringCase buyer
        enteredEvent.BuyerReferralReward |> should equal BigInteger.Zero
        enteredEvent.Sender |> shouldEqualIgnoringCase debug.ContractPlug.Address
        enteredEvent.Referrer |> shouldEqualIgnoringCase EthAddress.Zero
        enteredEvent.ReferrerReferralReward |> should equal BigInteger.Zero
        enteredEvent.ValueEntered |> should equal valueToEnter

    // state validation
    bucketSale.Query "treasury" [||] |> shouldEqualIgnoringCase treasury.Address
    bucketSale.Query "startOfSale" [||] |> should equal startOfSale
    bucketSale.Query "bucketPeriod" [||] |> should equal bucketPeriod
    bucketSale.Query "bucketSupply" [||] |> should equal bucketSupply
    bucketSale.Query "bucketCount" [||] |> should equal bucketCount
    bucketSale.Query "tokenOnSale" [||] |> shouldEqualIgnoringCase FRY.Address
    bucketSale.Query "tokenSoldFor" [||] |> shouldEqualIgnoringCase DAI.Address

    let senderDaiBalanceAfter = DAI.Query "balanceOf" [| debug.ContractPlug.Address |]
    senderDaiBalanceAfter |> should equal (senderDaiBalanceBefore - valueToEnter)
    let treasuryDaiBalanceAfter = DAI.Query "balanceOf" [| treasury.Address |]
    treasuryDaiBalanceAfter |> should equal (treasuryDaiBalanceBefore + valueToEnter)

    let buyForBucketAfter = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketToEnter; buyer |]
    buyForBucketAfter.ValueEntered |> should equal (buyForBucketBefore.ValueEntered + valueToEnter)
    buyForBucketAfter.BuyerTokensExited |> should equal BigInteger.Zero

    let bucketAfter = bucketSale.QueryObj<BucketsOutputDTO> "buckets" [| bucketToEnter |]
    bucketAfter.TotalValueEntered |> should equal (bucketBefore.TotalValueEntered + valueToEnter)

    if referrer <> EthAddress.Zero then
        let buyerRewardBuyAfter = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketToEnter + BigInteger.One; buyer |]
        buyerRewardBuyAfter.ValueEntered |> should equal (buyerRewardBuyBefore.ValueEntered + buyerReward)
        buyerRewardBuyAfter.BuyerTokensExited |> should equal BigInteger.Zero

        let referrerRewardBuyForBucketAfter = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketToEnter + BigInteger.One; referrer |]
        referrerRewardBuyForBucketAfter.ValueEntered |> should equal (referrerRewardBuyBefore.ValueEntered + referrerReward)
        referrerRewardBuyForBucketAfter.BuyerTokensExited |> should equal BigInteger.Zero

        let referralBucketAfter = bucketSale.QueryObj<BucketsOutputDTO> "buckets" [| bucketToEnter + BigInteger.One |]
        referralBucketAfter.TotalValueEntered |> should equal (referralBucketBefore.TotalValueEntered + referrerReward + buyerReward)
    else
        let buyerRewardBuyAfter = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketToEnter + BigInteger.One; buyer |]
        buyerRewardBuyAfter.ValueEntered |> should equal buyerRewardBuyBefore.ValueEntered
        buyerRewardBuyAfter.BuyerTokensExited |> should equal BigInteger.Zero

        let referrerRewardBuyForBucketAfter = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketToEnter + BigInteger.One; referrer |]
        referrerRewardBuyForBucketAfter.ValueEntered |> should equal referrerRewardBuyBefore.ValueEntered
        referrerRewardBuyForBucketAfter.BuyerTokensExited |> should equal BigInteger.Zero

        let referralBucketAfter = bucketSale.QueryObj<BucketsOutputDTO> "buckets" [| bucketToEnter + BigInteger.One |]
        referralBucketAfter.TotalValueEntered |> should equal (referralBucketBefore.TotalValueEntered)

        
let exitBucket buyer bucketEntered valueEntered =
    let buyerBalanceBefore = FRY.Query "balanceOf" [| buyer |]
    let totalSupplyBefore = FRY.Query "totalSupply" [||]
    let totalTokensExitedBefore = bucketSale.Query "totalExitedTokens" [||]

    let exitBucketReceipt = bucketSale.ExecuteFunction "exit" [| bucketEntered; buyer |] 
    exitBucketReceipt |> shouldSucceed

    let bucket = bucketSale.QueryObj<BucketsOutputDTO> "buckets" [| bucketEntered |]
    let amountToExit = 
        (bucketSupply * (BigInteger 100000UL) * valueEntered) / (bucket.TotalValueEntered * (BigInteger 100000UL))

    let exitEvent = exitBucketReceipt |> decodeFirstEvent<ExitedEventDTO>
    exitEvent.BucketId |> should equal bucketEntered
    exitEvent.Buyer |> shouldEqualIgnoringCase buyer
    exitEvent.TokensExited |> should equal amountToExit

    let buy = bucketSale.QueryObj<BuysOutputDTO> "buys" [| bucketEntered; buyer |]
    buy.BuyerTokensExited |> should equal amountToExit
    buy.ValueEntered |> should equal valueEntered

    let totalTokensExitedAfter = bucketSale.Query "totalExitedTokens" [||]
    totalTokensExitedAfter |> should equal (totalTokensExitedBefore + amountToExit) 
    
    let buyerBalanceAfter = FRY.Query "balanceOf" [| buyer |]
    buyerBalanceAfter |> should equal (buyerBalanceBefore + amountToExit)

    let totalSupplyAfter = FRY.Query "totalSupply" [||]
    totalSupplyAfter |> should equal (totalSupplyBefore + amountToExit)