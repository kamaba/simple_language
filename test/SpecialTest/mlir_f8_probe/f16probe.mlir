module {
  func.func @t(%a: i64) -> i64 {
    %b = llvm.bitcast %a : i64 to f64
    %t16 = arith.truncf %b : f64 to f16
    %w16 = arith.extf %t16 : f16 to f64
    %tbf = arith.truncf %b : f64 to bf16
    %wbf = arith.extf %tbf : bf16 to f64
    %s = arith.addf %w16, %wbf : f64
    %r = llvm.bitcast %s : f64 to i64
    return %r : i64
  }
}
