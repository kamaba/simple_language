module {
  func.func @t(%a: i64) -> i64 {
    %b = llvm.bitcast %a : i64 to f64
    %t8 = arith.truncf %b : f64 to f8E4M3FN
    %w8 = arith.extf %t8 : f8E4M3FN to f64
    %t5 = arith.truncf %b : f64 to f8E5M2
    %w5 = arith.extf %t5 : f8E5M2 to f64
    %t16 = arith.truncf %b : f64 to f16
    %w16 = arith.extf %t16 : f16 to f64
    %tbf = arith.truncf %b : f64 to bf16
    %wbf = arith.extf %tbf : bf16 to f64
    %s0 = arith.addf %w8, %w5 : f64
    %s1 = arith.addf %s0, %w16 : f64
    %s2 = arith.addf %s1, %wbf : f64
    %r = llvm.bitcast %s2 : f64 to i64
    return %r : i64
  }
}
