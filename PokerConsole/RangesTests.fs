﻿module RangesTests

open FsCheck
open FsCheck.Xunit
open Hands
open Ranges

[<Property>]
let ``isHandInRange returns false for pair range and non-pair hand`` (r : PairRange) h =
  not (h.Card1 = h.Card2) ==>
  not (isHandInRange (Pair r) h)