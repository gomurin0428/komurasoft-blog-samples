# 記事の受注の例（スキーマ、Δ、Σ）を1つの Julia のセッションで順に実行する
for f in ["01_schema.jl", "02_delta.jl", "03_sigma.jl"]
  println("===== ", f)
  include(joinpath(@__DIR__, f))
end
