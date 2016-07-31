﻿open System
open Excel.Import
open Hands
open Cards.Actions
open Cards.HandValues
open PostFlop.Options
open PostFlop.Decision
open PostFlop.Import
open PostFlop.Texture
open PostFlop.HandValue

[<EntryPoint>]
let main argv =   

  Console.Write "Opening excel files..."
  let fileNameTurnDonk = System.IO.Directory.GetCurrentDirectory() + @"\HandStrength.xlsx"
  let xlTurnDonk = openExcel fileNameTurnDonk
  Console.Write "\n"

  let mutable k = 'a'
  while k <> 'q' do
    Console.Write "\nPlease enter your hand (e.g. AsAc, 8d7h, Kh2h): "
    let handString = Console.ReadLine()
    let hand = parseFullHand handString
    let suitedHand = parseSuitedHand handString

    printf "\nPlease enter turn (e.g. 9s8c7d6h): "
    let boardString = Console.ReadLine()
    let board = parseBoard boardString
    let value = handValueWithDraws suitedHand board
    printfn "Hand value is: %A" value

    let texture = boardTexture board
    printfn "Special conditions: %A" texture

    let snapshot = { Hand = suitedHand; Board = board; Pot = 270; VillainStack = 350; HeroStack = 380; VillainBet = 90; HeroBet = 0; BB = 20 }
    let history = [
      { Action = Action.RaiseToAmount 40; Motivation = None; VsVillainBet = 20; Street = PreFlop }; 
      { Action = Action.RaiseToAmount 50; Motivation = None; VsVillainBet = 0; Street = Flop }]

    let turnDonkOption = importTurnDonk (fst xlTurnDonk) value texture snapshot history
    printf "Turn donk action is: %A.\nPress any key to continue or 'q' to exit:" turnDonkOption
    k <- Console.ReadKey().KeyChar

  closeExcel xlTurnDonk
  0 // return an integer exit code