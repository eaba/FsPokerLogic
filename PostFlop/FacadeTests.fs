﻿module FacadeTests

open Excel.Import
open Xunit
open Cards.Actions
open Hands
open Cards.HandValues
open PostFlop.Options
open PostFlop.Decision
open PostFlop.HandValue
open PostFlop.Facade
open PostFlop.Import

let defaultFlop = { Hand = { Card1 = {Face = Ace; Suit = Hearts}; Card2 = {Face = Five; Suit = Hearts} }; Board = [|{Face = Queen; Suit = Spades}; {Face = Ten; Suit = Clubs}; {Face = Six; Suit = Spades}|]; Pot = 80; VillainStack = 490; HeroStack = 430; VillainBet = 0; HeroBet = 0; BB = 20 }

let nml l = l |> List.map (fun nm -> (nm, None))

[<Fact>]
let ``pickOopSheet returns limp coorectly for single item`` () =
  let actual = pickOopSheet (nml [Action.Check]) defaultFlop
  Assert.Equal(Some "limp and check", actual)

[<Fact>]
let ``pickOopSheet returns limp coorectly for multiple items`` () =
  let actual = pickOopSheet (nml [Action.Check; Action.Call; Action.RaiseToAmount 100]) defaultFlop
  Assert.Equal(Some "limp and check", actual)

[<Fact>]
let ``pickOopSheet returns call coorectly for single items`` () =
  let actual = pickOopSheet (nml [Action.Call]) defaultFlop
  Assert.Equal(Some "hero call raise pre", actual)

[<Fact>]
let ``pickOopSheet returns skips SitBack`` () =
  let actual = pickOopSheet (nml [Action.SitBack; Action.Call]) defaultFlop
  Assert.Equal(Some "hero call raise pre", actual)

[<Fact>]
let ``pickOopSheet returns call coorectly for multiple items`` () =
  let actual = pickOopSheet (nml [Action.Call; Action.Call]) defaultFlop
  Assert.Equal(Some "hero call raise pre", actual)

[<Fact>]
let ``pickOopSheet returns raise FV coorectly for single item`` () =
  let actual = pickOopSheet (nml [Action.RaiseToAmount 60]) defaultFlop
  Assert.Equal(Some "hero raise FV vs limp", actual)

[<Fact>]
let ``pickOopSheet returns raise FV coorectly for multiple items`` () =
  let actual = pickOopSheet (nml [Action.RaiseToAmount 60; Action.RaiseToAmount 100]) defaultFlop
  Assert.Equal(Some "hero raise FV vs limp", actual)

[<Fact>]
let ``pickOopSheet returns raise FB coorectly for single item`` () =
  let actual = pickOopSheet [(Action.RaiseToAmount 60, Some Bluff)] defaultFlop
  Assert.Equal(Some "hero raise FB vs limp", actual)

[<Fact>]
let ``pickOopSheet falls back to limp for empty history and small pot`` () =
  let s = {defaultFlop with VillainBet = 30; Pot = 70}
  let actual = pickOopSheet [] s
  Assert.Equal(Some "limp and check", actual)

[<Fact>]
let ``pickOopSheet falls back to call for empty history and bigger pot`` () =
  let s = {defaultFlop with VillainBet = 30; Pot = 110}
  let actual = pickOopSheet [] s
  Assert.Equal(Some "hero call raise pre", actual)

let fileNameAdvancedOOP = System.IO.Directory.GetCurrentDirectory() + @"\PostflopPART2.xlsx"
let adxl = useExcel fileNameAdvancedOOP
let bluffy = 
  (importFlopList "bluffy hero ch-r flop vs limp" adxl.Workbook,
    importFlopList "bluffy hero ch-r flop vs minr" adxl.Workbook,
    importFlopList "bluffy overtaking, vill ch b fl" adxl.Workbook)

let testPostFlopMotivatedExt h s mono test =
  let v = handValueWithDraws s.Hand s.Board
  let t = { Streety = false; DoublePaired = false; ThreeOfKind = false; FourOfKind = false; Monoboard = mono }
  let riverBetSizing = [
    { MinPotSize = 0; MaxPotSize = 500; MinAllInPercentage = 40; MaxAllInPercentage = 70; MinChipsLeft = 50; BetSize = 60 }]

  let fileNameOop = System.IO.Directory.GetCurrentDirectory() + @"\PostflopOOP.xlsx"
  use xlOop = useExcel fileNameOop
  let fileNameTricky = System.IO.Directory.GetCurrentDirectory() + @"\tricky.xlsx"
  use xlTricky = useExcel fileNameTricky
  let actual = decidePostFlopOop h s v t xlOop.Workbook xlTricky.Workbook bluffy ((fun _ -> false), (fun _ -> false)) riverBetSizing
  test actual

let testPostFlopMotivated h s mono expected =
  testPostFlopMotivatedExt h s mono (fun x -> Assert.Equal(expected, x.Value.Action))

let testPostFlopExt h s mono test =
  let mh = h |> List.map (notMotivated Flop s.VillainBet)
  testPostFlopMotivatedExt mh s mono test

let testPostFlop h s mono expected =
  let mh = h |> List.map (notMotivated Flop s.VillainBet)
  testPostFlopMotivated mh s mono expected

[<Fact>]
let ``decidePostFlop bet on turn`` () =
  let s = { Hand = parseSuitedHand "TcQh"; Board = parseBoard "TsTd7sQs"; Pot = 240; VillainStack = 400; HeroStack = 400; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.RaiseToAmount 60; Action.RaiseToAmount 60] s 3 (Action.RaiseToAmount 150)

[<Fact>]
let ``decidePostFlop folds flop on 3 bet with 2nd + GS`` () =
  let s = { Hand = parseSuitedHand "8s7h"; Board = parseBoard "JcTh8d"; Pot = 570; VillainStack = 0; HeroStack = 300; VillainBet = 255; HeroBet = 90; BB = 20 }
  testPostFlop [Action.Check; Action.RaiseToAmount 90] s 0 (Action.Fold)

[<Fact>]
let ``decidePostFlop folds flop on 3 bet with OE`` () =
  let s = { Hand = parseSuitedHand "5h3d"; Board = parseBoard "8h2c4s"; Pot = 570; VillainStack = 0; HeroStack = 130; VillainBet = 685; HeroBet = 125; BB = 20 }
  testPostFlop [Action.Check; Action.RaiseToAmount 90] s 0 (Action.Fold)

[<Fact>]
let ``decidePostFlop ovso special rule on turn`` () =
  let s = { Hand = parseSuitedHand "7s8s"; Board = parseBoard "5s7c3hTd"; Pot = 200; VillainStack = 400; HeroStack = 400; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.RaiseToAmount 40; Action.RaiseToAmount 60] s 0 (Action.RaiseToAmount 135)

[<Fact>]
let ``decidePostFlop ovso special rule on turn 2`` () =
  let s = { Hand = parseSuitedHand "Jh9c"; Board = parseBoard "5sJc5dKs"; Pot = 240; VillainStack = 400; HeroStack = 400; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.RaiseToAmount 60; Action.RaiseToAmount 60] s 0 (Action.RaiseToAmount 160)

[<Fact>]
let ``decidePostFlop A/f special rule on turn`` () =
  let s = { Hand = parseSuitedHand "7sTs"; Board = parseBoard "8cKsJdAc"; Pot = 200; VillainStack = 300; HeroStack = 600; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.RaiseToAmount 40; Action.RaiseToAmount 60] s 0 (Action.RaiseToAmount 135)

[<Fact>]
let ``decidePostFlop 61 special rule on turn`` () =
  let s = { Hand = parseSuitedHand "Td7s"; Board = parseBoard "5hTh9sAs"; Pot = 220; VillainStack = 380; HeroStack = 400; VillainBet = 0; HeroBet = 0; BB = 30 }
  testPostFlop [Action.Check; Action.Check; Action.RaiseToAmount 80] s 0 (Action.RaiseToAmount 220)

[<Fact>]
let ``decidePostFlop 61 special rule on turn 2`` () =
  let s = { Hand = parseSuitedHand "7d5s"; Board = parseBoard "3d7h2cAh"; Pot = 80; VillainStack = 480; HeroStack = 480; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Check; Action.Check; Action.Call] s 0 (Action.RaiseToAmount 80)

[<Fact>]
let ``decidePostFlop 7 special rule on turn`` () =
  let s = { Hand = parseSuitedHand "8d9h"; Board = parseBoard "9cQd8s6h"; Pot = 100; VillainStack = 450; HeroStack = 450; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Call; Action.Check] s 0 (Action.RaiseToAmount 75)

[<Fact>]
let ``decidePostFlop 5 special rule on turn`` () =
  let s = { Hand = parseSuitedHand "QsKh"; Board = parseBoard "TdQc9d5h"; Pot = 100; VillainStack = 380; HeroStack = 520; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Call; Action.Check] s 0 (Action.RaiseToAmount 75)

[<Fact>]
let ``decidePostFlop give up turn after bluffy flop check raise`` () =
  let s = { Hand = parseSuitedHand "4hQc"; Board = parseBoard "Ts7c5d5h"; Pot = 300; VillainStack = 350; HeroStack = 350; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Call; Action.Check; Action.RaiseToAmount 110] s 0 Action.Check

[<Fact>]
let ``decidePostFlop cbet flop after raising limp pre`` () =
  let s = { Hand = parseSuitedHand "Qc9d"; Board = parseBoard "TdKs9c"; Pot = 120; VillainStack = 440; HeroStack = 440; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.RaiseToAmount 60] s 0 (Action.RaiseToAmount 60)

[<Fact>]
let ``decidePostFlop all in on turn after bluffy check/raise on flop`` () =
  let s = { Hand = parseSuitedHand "6h9h"; Board = parseBoard "Jd7h3d5s"; Pot = 300; VillainStack = 340; HeroStack = 340; VillainBet = 0; HeroBet = 0; BB = 20 }
  let h = [ { Action = Action.Call; Motivation = None; VsVillainBet = 40; Street = PreFlop }; 
            { Action = Action.Check; Motivation = None; VsVillainBet = 0; Street = Flop }; 
            { Action = Action.RaiseToAmount 110; Motivation = Some Bluff; VsVillainBet = 40; Street = Flop } ]
  testPostFlopMotivated h s 0 Action.AllIn


[<Fact>]
let ``decidePostFlop bet half pot on turn with FD+2nd pair`` () =
  let s = { Hand = parseSuitedHand "Ts8s"; Board = parseBoard "2dAs2sTc"; Pot = 80; VillainStack = 350; HeroStack = 350; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Call; Action.Check] s 0 (Action.RaiseToAmount 40)

[<Fact>]
let ``decidePostFlop bet turn for value after 3bet pf and bet flop`` () =
  let s = { Hand = parseSuitedHand "JhJs"; Board = parseBoard "3c2h6d7c"; Pot = 80; VillainStack = 300; HeroStack = 300; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.RaiseToAmount 100; Action.RaiseToAmount 100] s 0 Action.AllIn

[<Fact>]
let ``decidePostFlop cbet flop after bluffy 3bet pf`` () =
  let s = { Hand = parseSuitedHand "5c4d"; Board = parseBoard "AhKcJh"; Pot = 200; VillainStack = 400; HeroStack = 400; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlopMotivated [{ Action = Action.RaiseToAmount 100; Motivation = Some Bluff; VsVillainBet = 40; Street = Flop }] s 0 (Action.RaiseToAmount 100)

[<Fact>]
let ``decidePostFlop does not bluffy overtake turn after wrong flop`` () =
  let s = { Hand = parseSuitedHand "5c8c"; Board = parseBoard "7d4s3hTs"; Pot = 200; VillainStack = 610; HeroStack = 290; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Call; Action.Check] s 0 Action.Check

[<Fact>]
let ``decidePostFlop overtake turn on main rule 2`` () =
  let s = { Hand = parseSuitedHand "3d8h"; Board = parseBoard "7d3hTd6h"; Pot = 60; VillainStack = 590; HeroStack = 350; VillainBet = 0; HeroBet = 0; BB = 30 }
  testPostFlop [Action.Check; Action.Check] s 0 (Action.RaiseToAmount 45)

[<Fact>]
let ``decidePostFlop bet river after overtake turn on main rule 2`` () =
  let s = { Hand = parseSuitedHand "3sKh"; Board = parseBoard "3dJc9d2sAd"; Pot = 100; VillainStack = 340; HeroStack = 560; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Check; Action.Check; Action.RaiseToAmount 30] s 0 (Action.RaiseToAmount 50)

[<Fact>]
let ``decidePostFlop bet river after overtake turn on main rule 2 (2)`` () =
  let s = { Hand = parseSuitedHand "Qc4h"; Board = parseBoard "8h3h4dKsJh"; Pot = 100; VillainStack = 390; HeroStack = 610; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Check; Action.Check; Action.RaiseToAmount 30] s 0 (Action.RaiseToAmount 50)

[<Fact>]
let ``2`` () =
  let s = { Hand = parseSuitedHand "Qc4h"; Board = parseBoard "8h3h4dKsJh"; Pot = 100; VillainStack = 390; HeroStack = 610; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Check; Action.Check; Action.RaiseToAmount 30] s 0 (Action.RaiseToAmount 50)

[<Fact>]
let ``8`` () =
  let s = { Hand = parseSuitedHand "5s7c"; Board = parseBoard "4cAdQc"; Pot = 200; VillainStack = 370; HeroStack = 430; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlopMotivated [{ Action = Action.RaiseToAmount 100; Motivation = Some Bluff; VsVillainBet = 40; Street = Flop }] s 0 (Action.RaiseToAmount 100)

[<Fact>]
let ``13`` () =
  let s = { Hand = parseSuitedHand "9sJh"; Board = parseBoard "9cKs7c5d"; Pot = 272; VillainStack = 308; HeroStack = 420; VillainBet = 112; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Call; Action.Check; Action.Call; Action.Check] s 0 Action.Fold

[<Fact>]
let ``14f`` () =
  let s = { Hand = parseSuitedHand "4hJh"; Board = parseBoard "7d5s8s"; Pot = 120; VillainStack = 290; HeroStack = 590; VillainBet = 40; HeroBet = 0; BB = 20 }
  testPostFlop [Action.Call; Action.Check] s 0 (Action.RaiseToAmount 110)

[<Fact>]
let ``14`` () =
  let s = { Hand = parseSuitedHand "4hJh"; Board = parseBoard "7d5s8s6d"; Pot = 300; VillainStack = 220; HeroStack = 480; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlopMotivated [
    { Action = Action.Call; Motivation = None; VsVillainBet = 40; Street = PreFlop }; 
    { Action = Action.Check; Motivation = None; VsVillainBet = 0; Street = Flop }; 
    { Action = Action.RaiseToAmount 110; Motivation = Some Bluff; VsVillainBet = 40; Street = Flop }] s 0 Action.AllIn

[<Fact>]
let ``Turn OOP bet returns appropriate scenario name`` () =
  let s = { Hand = parseSuitedHand "4h5c"; Board = parseBoard "7d2s6dKs"; Pot = 80; VillainStack = 320; HeroStack = 580; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlopMotivatedExt [
    { Action = Action.Call; Motivation = None; VsVillainBet = 40; Street = PreFlop }; 
    { Action = Action.Check; Motivation = None; VsVillainBet = 0; Street = Flop }] s 0 (fun actual -> Assert.Equal(Some(Scenario "r8"), actual.Value.Motivation))

[<Fact>]
let ``River OOP bet is made based on turn scenario`` () =
  let s = { Hand = parseSuitedHand "4h5c"; Board = parseBoard "7d2s6dKsTh"; Pot = 160; VillainStack = 280; HeroStack = 440; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlopMotivated [
    { Action = Action.Call; Motivation = None; VsVillainBet = 40; Street = PreFlop }; 
    { Action = Action.Check; Motivation = None; VsVillainBet = 0; Street = Flop }; 
    { Action = Action.RaiseToAmount 40; Motivation = Some(Scenario "r8"); VsVillainBet = 00; Street = Turn }] s 0 (Action.RaiseToAmount 95)

[<Fact>]
let ``Flop OOP: float call`` () =
  let s = { Hand = parseSuitedHand "QdTh"; Board = parseBoard "2s7c2h"; Pot = 120; VillainStack = 330; HeroStack = 590; VillainBet = 40; HeroBet = 0; BB = 20 }
  testPostFlopExt [Action.Call; Action.Check] s 0 
    (fun actual -> Assert.Equal(Action.Call, actual.Value.Action); Assert.Equal(Some(Float BluffFloat), actual.Value.Motivation))

[<Fact>]
let ``Turn OOP: float call for bluffy`` () =
  let s = { Hand = parseSuitedHand "AdTh"; Board = parseBoard "2s7c2hJh"; Pot = 205; VillainStack = 285; HeroStack = 540; VillainBet = 45; HeroBet = 0; BB = 20 }
  testPostFlopMotivatedExt [
    { Action = Action.Call; Motivation = None; VsVillainBet = 40; Street = PreFlop }; 
    { Action = Action.Check; Motivation = None; VsVillainBet = 0; Street = Flop };
    { Action = Action.Call; Motivation = Some(Float BluffFloat); VsVillainBet = 40; Street = Flop };
    { Action = Action.Check; Motivation = None; VsVillainBet = 0; Street = Turn }] s 0 
    (fun actual -> Assert.Equal(Action.Call, actual.Value.Action); Assert.Equal(Some(Float BluffFloat), actual.Value.Motivation))

[<Fact>]
let ``River OOP: bet after floats`` () =
  let s = { Hand = parseSuitedHand "AdTh"; Board = parseBoard "2s7c2hJhTc"; Pot = 250; VillainStack = 285; HeroStack = 495; VillainBet = 0; HeroBet = 0; BB = 20 }
  testPostFlopMotivated [
    { Action = Action.Call; Motivation = None; VsVillainBet = 40; Street = PreFlop }; 
    { Action = Action.Check; Motivation = None; VsVillainBet = 0; Street = Flop };
    { Action = Action.Call; Motivation = Some(Float BluffFloat); VsVillainBet = 40; Street = Flop };
    { Action = Action.Check; Motivation = None; VsVillainBet = 0; Street = Turn };
    { Action = Action.Call; Motivation = Some(Float BluffFloat); VsVillainBet = 45; Street = Turn }] s 0 (Action.RaiseToAmount 155)


let fileNameFlopTurn = System.IO.Directory.GetCurrentDirectory() + @"\PostflopIP.xlsx"
let fileNameTurnDonk = System.IO.Directory.GetCurrentDirectory() + @"\HandStrength.xlsx"

let testIP s h expected =
  use xlFlopTurn = useExcel fileNameFlopTurn
  use xlTurnDonk = useExcel fileNameTurnDonk
  let v = handValueWithDraws s.Hand s.Board
  let t = { Streety = false; DoublePaired = false; ThreeOfKind = false; FourOfKind = false; Monoboard = 2 }
  let actual = decidePostFlop h s v t xlFlopTurn.Workbook xlTurnDonk.Workbook
  Assert.Equal(expected |> Some, actual)

[<Fact>]
let ``decidePostFlopIP cbet flush draw on turn`` () =
  let s = { Hand = parseSuitedHand "Jd8d"; Board = parseBoard "Th6d4dQc"; Pot = 120; VillainStack = 390; HeroStack = 450; VillainBet = 0; HeroBet = 0; BB = 20 }
  testIP s [] (Action.RaiseToAmount 90)

adxl.Dispose()