// Probe: integer bitwise implementation of the low-precision converts
// Convert_F8E4M3 / Convert_F8E5M2 / Convert_F16 / Convert_F16B
// (round-trip encode+decode of lp_float32_to_bits / lp_bits_to_float32).
// This is the exact op sequence the MLIRExporter will emit.
//
// f16/bf16 also use the integer path: llc (generic x86-64, no F16C/AVX512-BF16)
// lowers native f32<->f16/bf16 truncf/extf to compiler-rt soft-float calls
// (__truncsfhf2 / __extendhfsf2 / __truncsfbf2) which cannot be resolved by
// the CPU AOT dll link (libvcruntime only, no clang_rt in this LLVM build).
// The parameterized integer algorithm is bit-exact vs the C VM reference for
// all four formats; bf16 subnormal decode uses the (lmant << 16) variant
// (value < 2^-126 is subnormal in f32 too, C reference always takes the
// fs = e+149 = 16 branch there).
module {
  // ---- e4m3: ebits=4, mbits=3, bias=7, hasInf=false ----
  func.func @e4m3_encode(%x: f32) -> i32 {
    %c0 = arith.constant 0 : i32
    %c1 = arith.constant 1 : i32
    %c3 = arith.constant 3 : i32
    %c7 = arith.constant 7 : i32
    %c8 = arith.constant 8 : i32
    %c15 = arith.constant 15 : i32
    %c20 = arith.constant 20 : i32
    %c23 = arith.constant 23 : i32
    %c31 = arith.constant 31 : i32
    %c120 = arith.constant 120 : i32
    %c127 = arith.constant 127 : i32
    %c255 = arith.constant 255 : i32
    %halfN = arith.constant 524288 : i32
    %maskN = arith.constant 1048575 : i32
    %mantMask = arith.constant 8388607 : i32
    %implicitBit = arith.constant 8388608 : i32
    %baseN = arith.constant 21 : i32
    %baseS = arith.constant 20 : i32
    %b = llvm.bitcast %x : f32 to i32
    %sign = arith.shrui %b, %c31 : i32
    %e0 = arith.shrui %b, %c23 : i32
    %exp = arith.andi %e0, %c255 : i32
    %mant = arith.andi %b, %mantMask : i32
    %et = arith.subi %exp, %c120 : i32
    %signSh = arith.shli %sign, %c7 : i32
    %nanBits = arith.ori %signSh, %c127 : i32
    // normal path (et > 0)
    %m = arith.shrui %mant, %c20 : i32
    %rem = arith.andi %mant, %maskN : i32
    %remGt = arith.cmpi ugt, %rem, %halfN : i32
    %remEq = arith.cmpi eq, %rem, %halfN : i32
    %mLsb = arith.andi %m, %c1 : i32
    %mOdd = arith.cmpi ne, %mLsb, %c0 : i32
    %tie = arith.andi %remEq, %mOdd : i1
    %rndUp = arith.ori %remGt, %tie : i1
    %mInc = arith.addi %m, %c1 : i32
    %m2 = arith.select %rndUp, %mInc, %m : i32
    %carry = arith.cmpi eq, %m2, %c8 : i32
    %etInc = arith.addi %et, %c1 : i32
    %et2 = arith.select %carry, %etInc, %et : i32
    %m3 = arith.select %carry, %c0, %m2 : i32
    %et2Sh = arith.shli %et2, %c3 : i32
    %nBase = arith.ori %signSh, %et2Sh : i32
    %lpNok = arith.ori %nBase, %m3 : i32
    %ovf2 = arith.cmpi sgt, %et2, %c15 : i32
    %lpN = arith.select %ovf2, %nanBits, %lpNok : i32
    // subnormal path (et <= 0)
    %inN = arith.cmpi uge, %exp, %c1 : i32
    %mantHi = arith.ori %mant, %implicitBit : i32
    %sig = arith.select %inN, %mantHi, %mant : i32
    %base = arith.select %inN, %baseN, %baseS : i32
    %shift = arith.subi %base, %et : i32
    %lt1 = arith.cmpi slt, %shift, %c1 : i32
    %s1 = arith.select %lt1, %c1, %shift : i32
    %gt31 = arith.cmpi sgt, %s1, %c31 : i32
    %shiftC = arith.select %gt31, %c31, %s1 : i32
    %mm = llvm.lshr %sig, %shiftC : i32
    %restore = llvm.shl %mm, %shiftC : i32
    %remB = arith.subi %sig, %restore : i32
    %sc1 = arith.subi %shiftC, %c1 : i32
    %halfB = llvm.shl %c1, %sc1 : i32
    %rbGt = arith.cmpi ugt, %remB, %halfB : i32
    %rbEq = arith.cmpi eq, %remB, %halfB : i32
    %mmLsb = arith.andi %mm, %c1 : i32
    %mmOdd = arith.cmpi ne, %mmLsb, %c0 : i32
    %tie2 = arith.andi %rbEq, %mmOdd : i1
    %rndUp2 = arith.ori %rbGt, %tie2 : i1
    %mmInc = arith.addi %mm, %c1 : i32
    %mm2 = arith.select %rndUp2, %mmInc, %mm : i32
    %carry2 = arith.cmpi eq, %mm2, %c8 : i32
    %sOk = arith.ori %signSh, %mm2 : i32
    %sCar = arith.ori %signSh, %c8 : i32
    %lpS = arith.select %carry2, %sCar, %sOk : i32
    // combine: exp==255 or et>15 -> NaN slot; else et>0 ? normal : subnormal
    %isN = arith.cmpi sgt, %et, %c0 : i32
    %inner = arith.select %isN, %lpN, %lpS : i32
    %isInf = arith.cmpi eq, %exp, %c255 : i32
    %ovf = arith.cmpi sgt, %et, %c15 : i32
    %bad = arith.ori %isInf, %ovf : i1
    %lp = arith.select %bad, %nanBits, %inner : i32
    return %lp : i32
  }

  func.func @e4m3_decode(%lp: i32) -> f32 {
    %c0 = arith.constant 0 : i32
    %c3 = arith.constant 3 : i32
    %c7 = arith.constant 7 : i32
    %c15 = arith.constant 15 : i32
    %c20 = arith.constant 20 : i32
    %c23 = arith.constant 23 : i32
    %c31 = arith.constant 31 : i32
    %c120 = arith.constant 120 : i32
    %nan32 = arith.constant 2143289344 : i32
    %scale = arith.constant 0.001953125 : f32
    %lsign = arith.shrui %lp, %c7 : i32
    %le0 = arith.shrui %lp, %c3 : i32
    %lexp = arith.andi %le0, %c15 : i32
    %lmant = arith.andi %lp, %c7 : i32
    %rsign = arith.shli %lsign, %c31 : i32
    %lmZero = arith.cmpi eq, %lmant, %c0 : i32
    %lv = arith.sitofp %lmant : i32 to f32
    %lv2 = arith.mulf %lv, %scale : f32
    %lbits = llvm.bitcast %lv2 : f32 to i32
    %subVal = arith.ori %rsign, %lbits : i32
    %subOut = arith.select %lmZero, %rsign, %subVal : i32
    %leMax = arith.cmpi eq, %lexp, %c15 : i32
    %lmMax = arith.cmpi eq, %lmant, %c7 : i32
    %nanSlot = arith.andi %leMax, %lmMax : i1
    %fe = arith.addi %lexp, %c120 : i32
    %feSh = arith.shli %fe, %c23 : i32
    %lmSh = arith.shli %lmant, %c20 : i32
    %nBase = arith.ori %rsign, %feSh : i32
    %normOut = arith.ori %nBase, %lmSh : i32
    %notNan = arith.select %nanSlot, %nan32, %normOut : i32
    %leZero = arith.cmpi eq, %lexp, %c0 : i32
    %dec = arith.select %leZero, %subOut, %notNan : i32
    %out = llvm.bitcast %dec : i32 to f32
    return %out : f32
  }

  // ---- e5m2: ebits=5, mbits=2, bias=15, hasInf=true ----
  func.func @e5m2_encode(%x: f32) -> i32 {
    %c0 = arith.constant 0 : i32
    %c1 = arith.constant 1 : i32
    %c2 = arith.constant 2 : i32
    %c4 = arith.constant 4 : i32
    %c7 = arith.constant 7 : i32
    %c21 = arith.constant 21 : i32
    %c23 = arith.constant 23 : i32
    %c31 = arith.constant 31 : i32
    %c112 = arith.constant 112 : i32
    %c124 = arith.constant 124 : i32
    %c127 = arith.constant 127 : i32
    %c255 = arith.constant 255 : i32
    %halfN = arith.constant 1048576 : i32
    %maskN = arith.constant 2097151 : i32
    %mantMask = arith.constant 8388607 : i32
    %implicitBit = arith.constant 8388608 : i32
    %baseN = arith.constant 22 : i32
    %baseS = arith.constant 21 : i32
    %b = llvm.bitcast %x : f32 to i32
    %sign = arith.shrui %b, %c31 : i32
    %e0 = arith.shrui %b, %c23 : i32
    %exp = arith.andi %e0, %c255 : i32
    %mant = arith.andi %b, %mantMask : i32
    %et = arith.subi %exp, %c112 : i32
    %signSh = arith.shli %sign, %c7 : i32
    %infBits = arith.ori %signSh, %c124 : i32
    %nanBits = arith.ori %signSh, %c127 : i32
    %mantZero = arith.cmpi eq, %mant, %c0 : i32
    %lpIN = arith.select %mantZero, %infBits, %nanBits : i32
    %m = arith.shrui %mant, %c21 : i32
    %rem = arith.andi %mant, %maskN : i32
    %remGt = arith.cmpi ugt, %rem, %halfN : i32
    %remEq = arith.cmpi eq, %rem, %halfN : i32
    %mLsb = arith.andi %m, %c1 : i32
    %mOdd = arith.cmpi ne, %mLsb, %c0 : i32
    %tie = arith.andi %remEq, %mOdd : i1
    %rndUp = arith.ori %remGt, %tie : i1
    %mInc = arith.addi %m, %c1 : i32
    %m2 = arith.select %rndUp, %mInc, %m : i32
    %carry = arith.cmpi eq, %m2, %c4 : i32
    %etInc = arith.addi %et, %c1 : i32
    %et2 = arith.select %carry, %etInc, %et : i32
    %m3 = arith.select %carry, %c0, %m2 : i32
    %et2Sh = arith.shli %et2, %c2 : i32
    %nBase = arith.ori %signSh, %et2Sh : i32
    %lpNok = arith.ori %nBase, %m3 : i32
    %ovf2 = arith.cmpi sge, %et2, %c31 : i32
    %lpN = arith.select %ovf2, %infBits, %lpNok : i32
    %inN = arith.cmpi uge, %exp, %c1 : i32
    %mantHi = arith.ori %mant, %implicitBit : i32
    %sig = arith.select %inN, %mantHi, %mant : i32
    %base = arith.select %inN, %baseN, %baseS : i32
    %shift = arith.subi %base, %et : i32
    %lt1 = arith.cmpi slt, %shift, %c1 : i32
    %s1 = arith.select %lt1, %c1, %shift : i32
    %gt31 = arith.cmpi sgt, %s1, %c31 : i32
    %shiftC = arith.select %gt31, %c31, %s1 : i32
    %mm = llvm.lshr %sig, %shiftC : i32
    %restore = llvm.shl %mm, %shiftC : i32
    %remB = arith.subi %sig, %restore : i32
    %sc1 = arith.subi %shiftC, %c1 : i32
    %halfB = llvm.shl %c1, %sc1 : i32
    %rbGt = arith.cmpi ugt, %remB, %halfB : i32
    %rbEq = arith.cmpi eq, %remB, %halfB : i32
    %mmLsb = arith.andi %mm, %c1 : i32
    %mmOdd = arith.cmpi ne, %mmLsb, %c0 : i32
    %tie2 = arith.andi %rbEq, %mmOdd : i1
    %rndUp2 = arith.ori %rbGt, %tie2 : i1
    %mmInc = arith.addi %mm, %c1 : i32
    %mm2 = arith.select %rndUp2, %mmInc, %mm : i32
    %carry2 = arith.cmpi eq, %mm2, %c4 : i32
    %sOk = arith.ori %signSh, %mm2 : i32
    %sCar = arith.ori %signSh, %c4 : i32
    %lpS = arith.select %carry2, %sCar, %sOk : i32
    %isN = arith.cmpi sgt, %et, %c0 : i32
    %inner = arith.select %isN, %lpN, %lpS : i32
    %ovf = arith.cmpi sge, %et, %c31 : i32
    %notOvf = arith.select %ovf, %infBits, %inner : i32
    %isInf = arith.cmpi eq, %exp, %c255 : i32
    %lp = arith.select %isInf, %lpIN, %notOvf : i32
    return %lp : i32
  }

  func.func @e5m2_decode(%lp: i32) -> f32 {
    %c0 = arith.constant 0 : i32
    %c2 = arith.constant 2 : i32
    %c3 = arith.constant 3 : i32
    %c7 = arith.constant 7 : i32
    %c21 = arith.constant 21 : i32
    %c23 = arith.constant 23 : i32
    %c31 = arith.constant 31 : i32
    %c112 = arith.constant 112 : i32
    %nan32 = arith.constant 2143289344 : i32
    %inf32 = arith.constant 2139095040 : i32
    %scale = arith.constant 0.0000152587890625 : f32
    %lsign = arith.shrui %lp, %c7 : i32
    %le0 = arith.shrui %lp, %c2 : i32
    %lexp = arith.andi %le0, %c31 : i32
    %lmant = arith.andi %lp, %c3 : i32
    %rsign = arith.shli %lsign, %c31 : i32
    %lmZero = arith.cmpi eq, %lmant, %c0 : i32
    %lv = arith.sitofp %lmant : i32 to f32
    %lv2 = arith.mulf %lv, %scale : f32
    %lbits = llvm.bitcast %lv2 : f32 to i32
    %subVal = arith.ori %rsign, %lbits : i32
    %subOut = arith.select %lmZero, %rsign, %subVal : i32
    %leMax = arith.cmpi eq, %lexp, %c31 : i32
    %lmZero2 = arith.cmpi eq, %lmant, %c0 : i32
    %infVal = arith.ori %rsign, %inf32 : i32
    %specOut = arith.select %lmZero2, %infVal, %nan32 : i32
    %fe = arith.addi %lexp, %c112 : i32
    %feSh = arith.shli %fe, %c23 : i32
    %lmSh = arith.shli %lmant, %c21 : i32
    %nBase = arith.ori %rsign, %feSh : i32
    %normOut = arith.ori %nBase, %lmSh : i32
    %leZero = arith.cmpi eq, %lexp, %c0 : i32
    %notSub = arith.select %leMax, %specOut, %normOut : i32
    %dec = arith.select %leZero, %subOut, %notSub : i32
    %out = llvm.bitcast %dec : i32 to f32
    return %out : f32
  }

  // ---- full convert round trips (VM semantics: f64 -> (f32) -> lp bits -> f32 -> f64) ----
  func.func @e4m3_rt(%d: f64) -> i64 {
    %t32 = arith.truncf %d : f64 to f32
    %lp = func.call @e4m3_encode(%t32) : (f32) -> i32
    %w32 = func.call @e4m3_decode(%lp) : (i32) -> f32
    %w = arith.extf %w32 : f32 to f64
    %r = llvm.bitcast %w : f64 to i64
    return %r : i64
  }

  func.func @e5m2_rt(%d: f64) -> i64 {
    %t32 = arith.truncf %d : f64 to f32
    %lp = func.call @e5m2_encode(%t32) : (f32) -> i32
    %w32 = func.call @e5m2_decode(%lp) : (i32) -> f32
    %w = arith.extf %w32 : f32 to f64
    %r = llvm.bitcast %w : f64 to i64
    return %r : i64
  }

  // ---- f16: ebits=5, mbits=10, bias=15, hasInf=true ----
  func.func @f16_encode(%x: f32) -> i32 {
    %c0 = arith.constant 0 : i32
    %c1 = arith.constant 1 : i32
    %c10 = arith.constant 10 : i32
    %c13 = arith.constant 13 : i32
    %c15 = arith.constant 15 : i32
    %c23 = arith.constant 23 : i32
    %c31 = arith.constant 31 : i32
    %c112 = arith.constant 112 : i32
    %c255 = arith.constant 255 : i32
    %c1024 = arith.constant 1024 : i32
    %c31744 = arith.constant 31744 : i32
    %c32767 = arith.constant 32767 : i32
    %halfN = arith.constant 4096 : i32
    %maskN = arith.constant 8191 : i32
    %mantMask = arith.constant 8388607 : i32
    %implicitBit = arith.constant 8388608 : i32
    %baseN = arith.constant 14 : i32
    %baseS = arith.constant 13 : i32
    %b = llvm.bitcast %x : f32 to i32
    %sign = arith.shrui %b, %c31 : i32
    %e0 = arith.shrui %b, %c23 : i32
    %exp = arith.andi %e0, %c255 : i32
    %mant = arith.andi %b, %mantMask : i32
    %et = arith.subi %exp, %c112 : i32
    %signSh = arith.shli %sign, %c15 : i32
    %infBits = arith.ori %signSh, %c31744 : i32
    %nanBits = arith.ori %signSh, %c32767 : i32
    %mantZero = arith.cmpi eq, %mant, %c0 : i32
    %lpIN = arith.select %mantZero, %infBits, %nanBits : i32
    %m = arith.shrui %mant, %c13 : i32
    %rem = arith.andi %mant, %maskN : i32
    %remGt = arith.cmpi ugt, %rem, %halfN : i32
    %remEq = arith.cmpi eq, %rem, %halfN : i32
    %mLsb = arith.andi %m, %c1 : i32
    %mOdd = arith.cmpi ne, %mLsb, %c0 : i32
    %tie = arith.andi %remEq, %mOdd : i1
    %rndUp = arith.ori %remGt, %tie : i1
    %mInc = arith.addi %m, %c1 : i32
    %m2 = arith.select %rndUp, %mInc, %m : i32
    %carry = arith.cmpi eq, %m2, %c1024 : i32
    %etInc = arith.addi %et, %c1 : i32
    %et2 = arith.select %carry, %etInc, %et : i32
    %m3 = arith.select %carry, %c0, %m2 : i32
    %et2Sh = arith.shli %et2, %c10 : i32
    %nBase = arith.ori %signSh, %et2Sh : i32
    %lpNok = arith.ori %nBase, %m3 : i32
    %ovf2 = arith.cmpi sge, %et2, %c31 : i32
    %lpN = arith.select %ovf2, %infBits, %lpNok : i32
    %inN = arith.cmpi uge, %exp, %c1 : i32
    %mantHi = arith.ori %mant, %implicitBit : i32
    %sig = arith.select %inN, %mantHi, %mant : i32
    %base = arith.select %inN, %baseN, %baseS : i32
    %shift = arith.subi %base, %et : i32
    %lt1 = arith.cmpi slt, %shift, %c1 : i32
    %s1 = arith.select %lt1, %c1, %shift : i32
    %gt31 = arith.cmpi sgt, %s1, %c31 : i32
    %shiftC = arith.select %gt31, %c31, %s1 : i32
    %mm = llvm.lshr %sig, %shiftC : i32
    %restore = llvm.shl %mm, %shiftC : i32
    %remB = arith.subi %sig, %restore : i32
    %sc1 = arith.subi %shiftC, %c1 : i32
    %halfB = llvm.shl %c1, %sc1 : i32
    %rbGt = arith.cmpi ugt, %remB, %halfB : i32
    %rbEq = arith.cmpi eq, %remB, %halfB : i32
    %mmLsb = arith.andi %mm, %c1 : i32
    %mmOdd = arith.cmpi ne, %mmLsb, %c0 : i32
    %tie2 = arith.andi %rbEq, %mmOdd : i1
    %rndUp2 = arith.ori %rbGt, %tie2 : i1
    %mmInc = arith.addi %mm, %c1 : i32
    %mm2 = arith.select %rndUp2, %mmInc, %mm : i32
    %carry2 = arith.cmpi eq, %mm2, %c1024 : i32
    %sOk = arith.ori %signSh, %mm2 : i32
    %sCar = arith.ori %signSh, %c1024 : i32
    %lpS = arith.select %carry2, %sCar, %sOk : i32
    %isN = arith.cmpi sgt, %et, %c0 : i32
    %inner = arith.select %isN, %lpN, %lpS : i32
    %ovf = arith.cmpi sge, %et, %c31 : i32
    %notOvf = arith.select %ovf, %infBits, %inner : i32
    %isInf = arith.cmpi eq, %exp, %c255 : i32
    %lp = arith.select %isInf, %lpIN, %notOvf : i32
    return %lp : i32
  }

  func.func @f16_decode(%lp: i32) -> f32 {
    %c0 = arith.constant 0 : i32
    %c10 = arith.constant 10 : i32
    %c13 = arith.constant 13 : i32
    %c15 = arith.constant 15 : i32
    %c23 = arith.constant 23 : i32
    %c31 = arith.constant 31 : i32
    %c112 = arith.constant 112 : i32
    %c1023 = arith.constant 1023 : i32
    %nan32 = arith.constant 2143289344 : i32
    %inf32 = arith.constant 2139095040 : i32
    %scale = arith.constant 0.000000059604644775390625 : f32
    %lsign = arith.shrui %lp, %c15 : i32
    %le0 = arith.shrui %lp, %c10 : i32
    %lexp = arith.andi %le0, %c31 : i32
    %lmant = arith.andi %lp, %c1023 : i32
    %rsign = arith.shli %lsign, %c31 : i32
    %lmZero = arith.cmpi eq, %lmant, %c0 : i32
    %lv = arith.sitofp %lmant : i32 to f32
    %lv2 = arith.mulf %lv, %scale : f32
    %lbits = llvm.bitcast %lv2 : f32 to i32
    %subVal = arith.ori %rsign, %lbits : i32
    %subOut = arith.select %lmZero, %rsign, %subVal : i32
    %leMax = arith.cmpi eq, %lexp, %c31 : i32
    %lmZero2 = arith.cmpi eq, %lmant, %c0 : i32
    %infVal = arith.ori %rsign, %inf32 : i32
    %specOut = arith.select %lmZero2, %infVal, %nan32 : i32
    %fe = arith.addi %lexp, %c112 : i32
    %feSh = arith.shli %fe, %c23 : i32
    %lmSh = arith.shli %lmant, %c13 : i32
    %nBase = arith.ori %rsign, %feSh : i32
    %normOut = arith.ori %nBase, %lmSh : i32
    %leZero = arith.cmpi eq, %lexp, %c0 : i32
    %notSub = arith.select %leMax, %specOut, %normOut : i32
    %dec = arith.select %leZero, %subOut, %notSub : i32
    %out = llvm.bitcast %dec : i32 to f32
    return %out : f32
  }

  // ---- bf16: ebits=8, mbits=7, bias=127, hasInf=true ----
  // et = exp - (127 - bias) = exp (shares the f32 exponent field).
  // Subnormal encode: only f32-subnormal inputs (exp==0) reach it,
  // shift = 23 - 7 - 0 = 16, mm = mant >> 16 <= 127 (never carries).
  // Subnormal decode: value = lmant * 2^-133 < 2^-126 is subnormal in f32
  // too, C reference always takes the fs = e+149 = 16 branch:
  // bits = rsign | (lmant << 16).
  func.func @bf16_encode(%x: f32) -> i32 {
    %c0 = arith.constant 0 : i32
    %c1 = arith.constant 1 : i32
    %c7 = arith.constant 7 : i32
    %c15 = arith.constant 15 : i32
    %c16 = arith.constant 16 : i32
    %c23 = arith.constant 23 : i32
    %c31 = arith.constant 31 : i32
    %c128 = arith.constant 128 : i32
    %c255 = arith.constant 255 : i32
    %c32640 = arith.constant 32640 : i32
    %c32767 = arith.constant 32767 : i32
    %halfN = arith.constant 32768 : i32
    %maskN = arith.constant 65535 : i32
    %mantMask = arith.constant 8388607 : i32
    %implicitBit = arith.constant 8388608 : i32
    %baseN = arith.constant 17 : i32
    %baseS = arith.constant 16 : i32
    %b = llvm.bitcast %x : f32 to i32
    %sign = arith.shrui %b, %c31 : i32
    %e0 = arith.shrui %b, %c23 : i32
    %exp = arith.andi %e0, %c255 : i32
    %mant = arith.andi %b, %mantMask : i32
    %et = arith.subi %exp, %c0 : i32
    %signSh = arith.shli %sign, %c15 : i32
    %infBits = arith.ori %signSh, %c32640 : i32
    %nanBits = arith.ori %signSh, %c32767 : i32
    %mantZero = arith.cmpi eq, %mant, %c0 : i32
    %lpIN = arith.select %mantZero, %infBits, %nanBits : i32
    %m = arith.shrui %mant, %c16 : i32
    %rem = arith.andi %mant, %maskN : i32
    %remGt = arith.cmpi ugt, %rem, %halfN : i32
    %remEq = arith.cmpi eq, %rem, %halfN : i32
    %mLsb = arith.andi %m, %c1 : i32
    %mOdd = arith.cmpi ne, %mLsb, %c0 : i32
    %tie = arith.andi %remEq, %mOdd : i1
    %rndUp = arith.ori %remGt, %tie : i1
    %mInc = arith.addi %m, %c1 : i32
    %m2 = arith.select %rndUp, %mInc, %m : i32
    %carry = arith.cmpi eq, %m2, %c128 : i32
    %etInc = arith.addi %et, %c1 : i32
    %et2 = arith.select %carry, %etInc, %et : i32
    %m3 = arith.select %carry, %c0, %m2 : i32
    %et2Sh = arith.shli %et2, %c7 : i32
    %nBase = arith.ori %signSh, %et2Sh : i32
    %lpNok = arith.ori %nBase, %m3 : i32
    %ovf2 = arith.cmpi sge, %et2, %c255 : i32
    %lpN = arith.select %ovf2, %infBits, %lpNok : i32
    %inN = arith.cmpi uge, %exp, %c1 : i32
    %mantHi = arith.ori %mant, %implicitBit : i32
    %sig = arith.select %inN, %mantHi, %mant : i32
    %base = arith.select %inN, %baseN, %baseS : i32
    %shift = arith.subi %base, %et : i32
    %lt1 = arith.cmpi slt, %shift, %c1 : i32
    %s1 = arith.select %lt1, %c1, %shift : i32
    %gt31 = arith.cmpi sgt, %s1, %c31 : i32
    %shiftC = arith.select %gt31, %c31, %s1 : i32
    %mm = llvm.lshr %sig, %shiftC : i32
    %restore = llvm.shl %mm, %shiftC : i32
    %remB = arith.subi %sig, %restore : i32
    %sc1 = arith.subi %shiftC, %c1 : i32
    %halfB = llvm.shl %c1, %sc1 : i32
    %rbGt = arith.cmpi ugt, %remB, %halfB : i32
    %rbEq = arith.cmpi eq, %remB, %halfB : i32
    %mmLsb = arith.andi %mm, %c1 : i32
    %mmOdd = arith.cmpi ne, %mmLsb, %c0 : i32
    %tie2 = arith.andi %rbEq, %mmOdd : i1
    %rndUp2 = arith.ori %rbGt, %tie2 : i1
    %mmInc = arith.addi %mm, %c1 : i32
    %mm2 = arith.select %rndUp2, %mmInc, %mm : i32
    %carry2 = arith.cmpi eq, %mm2, %c128 : i32
    %sOk = arith.ori %signSh, %mm2 : i32
    %sCar = arith.ori %signSh, %c128 : i32
    %lpS = arith.select %carry2, %sCar, %sOk : i32
    %isN = arith.cmpi sgt, %et, %c0 : i32
    %inner = arith.select %isN, %lpN, %lpS : i32
    %ovf = arith.cmpi sge, %et, %c255 : i32
    %notOvf = arith.select %ovf, %infBits, %inner : i32
    %isInf = arith.cmpi eq, %exp, %c255 : i32
    %lp = arith.select %isInf, %lpIN, %notOvf : i32
    return %lp : i32
  }

  func.func @bf16_decode(%lp: i32) -> f32 {
    %c0 = arith.constant 0 : i32
    %c7 = arith.constant 7 : i32
    %c15 = arith.constant 15 : i32
    %c16 = arith.constant 16 : i32
    %c23 = arith.constant 23 : i32
    %c31 = arith.constant 31 : i32
    %c127 = arith.constant 127 : i32
    %c255 = arith.constant 255 : i32
    %nan32 = arith.constant 2143289344 : i32
    %inf32 = arith.constant 2139095040 : i32
    %lsign = arith.shrui %lp, %c15 : i32
    %le0 = arith.shrui %lp, %c7 : i32
    %lexp = arith.andi %le0, %c255 : i32
    %lmant = arith.andi %lp, %c127 : i32
    %rsign = arith.shli %lsign, %c31 : i32
    %lmZero = arith.cmpi eq, %lmant, %c0 : i32
    %subBits = arith.shli %lmant, %c16 : i32
    %subVal = arith.ori %rsign, %subBits : i32
    %subOut = arith.select %lmZero, %rsign, %subVal : i32
    %leMax = arith.cmpi eq, %lexp, %c255 : i32
    %lmZero2 = arith.cmpi eq, %lmant, %c0 : i32
    %infVal = arith.ori %rsign, %inf32 : i32
    %specOut = arith.select %lmZero2, %infVal, %nan32 : i32
    %fe = arith.addi %lexp, %c0 : i32
    %feSh = arith.shli %fe, %c23 : i32
    %lmSh = arith.shli %lmant, %c16 : i32
    %nBase = arith.ori %rsign, %feSh : i32
    %normOut = arith.ori %nBase, %lmSh : i32
    %leZero = arith.cmpi eq, %lexp, %c0 : i32
    %notSub = arith.select %leMax, %specOut, %normOut : i32
    %dec = arith.select %leZero, %subOut, %notSub : i32
    %out = llvm.bitcast %dec : i32 to f32
    return %out : f32
  }

  func.func @f16_rt(%d: f64) -> i64 {
    %t32 = arith.truncf %d : f64 to f32
    %lp = func.call @f16_encode(%t32) : (f32) -> i32
    %w32 = func.call @f16_decode(%lp) : (i32) -> f32
    %w = arith.extf %w32 : f32 to f64
    %r = llvm.bitcast %w : f64 to i64
    return %r : i64
  }

  func.func @bf16_rt(%d: f64) -> i64 {
    %t32 = arith.truncf %d : f64 to f32
    %lp = func.call @bf16_encode(%t32) : (f32) -> i32
    %w32 = func.call @bf16_decode(%lp) : (i32) -> f32
    %w = arith.extf %w32 : f32 to f64
    %r = llvm.bitcast %w : f64 to i64
    return %r : i64
  }
}
