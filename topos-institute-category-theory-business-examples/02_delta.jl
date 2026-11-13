# 新スキーマ: 顧客に地域（Region）を足した
@present SchOrdersV2 <: SchOrders begin
  Region::Ob
  region::Hom(Customer, Region)
  region_name::Attr(Region, Name)
end

@acset_type OrdersV2(SchOrdersV2)

# 旧スキーマを新スキーマへ埋め込む関手 F（同じ名前の生成元どうしを対応させる）
C, D = FinCat(SchOrders), FinCat(SchOrdersV2)
in_D(gens) = Dict(nameof(g) => g for g in gens)
Dob, Dhom = in_D(ob_generators(D)), in_D(hom_generators(D))
F = FinFunctor(
  Dict(x => Dob[nameof(x)] for x in ob_generators(C)),
  Dict(f => Dhom[nameof(f)] for f in hom_generators(C)),
  C, D; homtype=:generator)
println("F は関手か: ", is_functorial(F))

# わざと誤った対応 G: 外部キー customer（受注 → 顧客）を region（顧客 → 地域）に送る
G = FinFunctor(
  Dict(x => Dob[nameof(x)] for x in ob_generators(C)),
  Dict(f => Dhom[nameof(f) == :customer ? :region : nameof(f)] for f in hom_generators(C)),
  C, D; homtype=:generator)
println("G は関手か: ", is_functorial(G))

# 新スキーマのデータ
v2 = @acset OrdersV2{String, Int, Int} begin
  Region = 2
  region_name = ["関東", "九州"]
  Customer = 2
  customer_name = ["A社", "B社"]
  region = [1, 2]
  Product = 2
  product_name = ["ねじ", "ナット"]
  price = [12, 8]
  Order = 3
  customer = [1, 2, 1]
  product = [1, 1, 2]
  quantity = [100, 50, 200]
end

# Δ_F: 新スキーマのデータを旧スキーマで読む（地域は落ちる）
old = migrate(Orders{String, Int, Int}, v2, DeltaMigration(F))
println("Δ で旧スキーマへ: 顧客 $(nparts(old, :Customer)) 件、受注 $(nparts(old, :Order)) 件")
println("顧客名: ", old[:customer_name])
println("旧スキーマの表: ", objects(acset_schema(old)))
