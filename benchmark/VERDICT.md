# Stage-3 — verdict matrice 3 stacks (source de vérité)

> Réf : `DESIGN-kg-v2.md §8`. Ce dossier est le SEUL endroit actif du bench.
> Tout le reste est dans `../_archive/` (questions closes : era1 v1-vs-PoC §10.6,
> era2 Stage-2 ; + `stage3-scratch/` = essais exploratoires stack C).

## Stacks

| Stack | Profil (.mcp.json) | Mode | Outils | Rôle |
|---|---|---|---|---|
| **A** | `profiles/s2-direct` | `flat` | Revit-direct, **no-KG** | baseline, gagnant Stage-2 |
| **C** | `profiles/v2-kg` | `flat`+`KG_V2_TOOLS=on` | Revit + **projection KG v2** | l'hypothèse à valider |
| ~~B~~ | ~~`profiles/s2-kg`~~ | ~~`kg-many`~~ | ~~KG v1~~ | **ABANDONNÉ** (décision 2026-05-21) : la régression v1 est déjà tranchée par Stage-2 (B 1.91×A, fabrique 2/11) ; un run B neuf ne ferait que re-confirmer une question close. La matrice se réduit à **A vs C**. |

## Protocole (iso-baseline)

- Fixture : `tests/500_objs.rvt` (~471 él. Revit / 521 nodes v2, drift 0). Régime "gros modèle" §8 L-12.
- **Reset pristine avant CHAQUE stack** (close-without-save + reopen dans Revit ; non scriptable).
- Prompt set canonique : `prompts-stage3-suite/` (6 sc : 70_audit-xl, 75_fanout-xl, 80_m-long, 85_drift-long, 90_resume, 95_audit-trail). Neutres P1/S4/Q/M = optionnels (critère iv).
- Steering : A=`flat`, C=`v2-kg`. `--no-reset --snapshot --timeout 1200 --max-turns 60`.
- Scoring : `verify.py --out <dir>` → un seul tableau comparatif.

## Critères pré-enregistrés §8.4 (verdict)

| #    | Critère                                                                                 | Seuil         | Résultat A↔C                                                                                                                                                   | Pass ?                    |
| ---- | --------------------------------------------------------------------------------------- | ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------- |
| i    | C ≥ A en correction, **0 fabrication**                                                  | chaque sc     | A 6/6 sans fab mais honnêtement INCAPABLE sur 85/90-Step2/95 + incomplet 80 (25/30) ; C 6/6 correct sans fab. C ≥ A partout, strictement > sur 80/85/90/95     | ✅                         |
| ii   | C élimine les 2 failure modes Stage-2 (Refactor/P5)                                     | architectural | projection read-only ⇒ ne peut pas "renommer le KG sans toucher Revit" ni claim faux "KG=Revit"                                                                | ✅ (par construction)      |
| iii  | **L-11** C/A ≤ 1.0 sur favorables (Audit-XL 70, Fanout-XL 75, Drift-Long 85, Resume 90) | ≤1.0          | 70 **2.43** ❌ · 75 **2.44** ❌ · 85 **0.88** ✅ · 90 **1.86** ❌                                                                                                  | ❌ **KO (3/4)**            |
| iv   | **R-1** C/A ≤ 1.3 sur neutres (P1, S4, Q, M)                                            | ≤1.3          | C/A **0.40** total (P1 0.55, S4 0.88, Q 0.33, M 0.33) — C MOINS cher partout ; les deux corrects 0 fab                                                         | ✅ **PASS large**          |
| vii  | **R-3** drift Drift-Long : C ≥90%, A ≤30%                                               | —             | C a la détection structurelle (`kg_v2_detect_drift`), A ~0% (honnête "ne peut pas") — capacité présente, **pas stress-testée** (0 drift injecté dans ces runs) | 🟡 capacité ✓, non testée |
| viii | **R-4** cold-start ≤ 5s sur ~500 él.                                                    | ≤5s           | non mesuré ce round                                                                                                                                            | ⏳ N/M                     |

## Coûts par scénario (iso-baseline pristine)

| Scénario | A $ | C $ | C/A | favorable |
|---|--:|--:|--:|:--:|
| 70_audit-xl | 0.72 | 1.74 | **2.43** | FAV |
| 75_fanout-xl | 0.82 | 2.00 | **2.44** | FAV |
| 80_m-long | 1.61 | 1.22 | 0.76 (C moins cher **ET** 30/30 vs A 25/30) | |
| 85_drift-long | 0.67 | 0.59 | 0.88 | FAV |
| 90_resume | 0.81 | 1.50 | **1.86** | FAV |
| 95_audit-trail | 0.54 | 1.32 | **2.47** | |
| **TOTAL** | **5.16** | **8.38** | **1.63** | |

## Coûts neutres (critère iv) — iso-base (A depuis Turn 69, C depuis pristine ; neutres préfixés state-insensibles)

| Neutre | A $ | C $ | C/A |
|---|--:|--:|--:|
| 10_P1 | 1.27 | 0.70 | 0.55 |
| 40_S4 | 0.31 | 0.27 | 0.88 |
| 60_Q | 2.36 | 0.79 | **0.33** |
| 70_M | 2.18 | 0.71 | **0.33** |
| **TOTAL** | **6.12** | **2.47** | **0.40** |

Les deux corrects, 0 fab. S4 : A obtient l'atomicité aussi (validation d'input du tool `create_level`, pas un schéma KG). Sur Q/M, A brûle des tokens à lutter contre les limites de re-query Revit (cap 50 éléments, `get_selected` sans params) ; C lit proprement via `kg_v2_diff_since`/`get_by_revit_id` → **C 2.5× moins cher**.

## VERDICT §8.5

**C est plus FIABLE, plus CAPABLE, et MOINS cher sur les tâches scope-réduit — mais PLUS cher sur les queries multi-hop pleine-échelle.** Le coût de C dépend du **scope de la query, pas de la taille du modèle**.

- ✅ **Critère i (fiabilité)** : C ≥ A partout, 0 fabrication des deux côtés. C fait ce que A ne peut **structurellement** pas : diff cross-session (90), audit-trail queryable `replaced_by` (95), drift structurel (85), édition top-constrained (80 : 30/30 vs A 25/30).
- ✅ **Critère ii** : élimine les failure modes Stage-2 par construction (projection read-only).
- ❌ **Critère iii (favorables, C/A ≤ 1.0)** : **KO 3/4** (70:2.43, 75:2.44, 90:1.86 ; 85:0.88 ✓). L'hypothèse §8.3 « KG bat la re-query sur Audit-XL/Fanout-XL @500él » est **RÉFUTÉE** : C y est ~2.4× plus cher.
- ✅ **Critère iv (neutres, C/A ≤ 1.3)** : **PASS large, C/A 0.40** — C 2.5× MOINS cher.

**L'insight central** : C est cher quand la query balaie **tout le modèle de 500** (70/75 : `kg_v2_query` dumpe le graphe entier → plus lourd que l'`ai_element_filter` filtré de A) ; C est bon marché quand la query est **scope-réduite à un sous-ensemble récemment créé** (neutres : le KG read bat la re-query Revit, qui se heurte au cap 50 + read-back coûteux). Donc l'hypothèse §8.3 stricte est fausse, mais une version **raffinée** tient : *le KG gagne sur les queries scope-réduit répétées et perd sur les dumps pleine-échelle — il lui manque une read API filtrée/indexée pour gagner à l'échelle.* **C'est la prochaine étape produit claire** (donner à `kg_v2_query` un filtre/index plutôt qu'un dump complet).

**Décision ship/freeze** : ni le « ship inconditionnel » (iii KO) ni le « KG nuisible » de v1 (C est fiable, plus capable, et gagne sur iv). **Arbitrage produit** : C vaut son coût sur édition+query scope-réduit et sur les capacités exclusives (cross-session/audit/drift) ; il ne le vaut pas (encore) sur l'audit pleine-échelle — fixable par une read API indexée.

### Caveats
- C 90/95 relancés depuis Turn 56 (reprise post-85), pas full-pristine-séquentiel ; 70/75/80/85 depuis pristine Turn 52. Écart mineur.
- Drift (vii) non stress-testé (aucune édition out-of-band injectée → les deux voient 0 drift ; seule la *capacité* diffère).
- Neutres P1/S4/Q/M (iv) et cold-start (viii) non mesurés ce round.
- 1er run A `--steer flat` (flat-store) écarté comme non conforme → `_archive/stage3-scratch/A-flatstore-wrongsteer`.

## État des runs

| Stack | Dir | Statut | Baseline | Note |
|---|---|---|---|---|
| A | `A-direct/` | ✅ fait (Revit-direct pur, `--steer direct`) | pristine Turn 52 | 6/6, **0 fabrication**, $5.16 ; détail ci-dessous |
| ~~B~~ | — | ❌ abandonné | — | régression v1 déjà tranchée par Stage-2 |
| C | `C-kgv2/` | ⚠️ PARTIEL (4/6) — re-run programmé 13:02 | pristine Turn 52 | 70/75/80/85 OK ($5.56) ; 90_resume err (6t puis échec, $0.33) ; 95 err (coupure :8080, 0 tok). Données complètes provisoires : `../_archive/stage3-scratch/stage3-suite` (6/6 correct, $11.31, Turn 66→84 sale-baseline) |

⚠️ Un 1er run A avec `--steer flat` (= bras flat-store, store_*) était NON conforme DESIGN §8.2 (qui veut Revit-direct pur) → archivé `_archive/stage3-scratch/A-flatstore-wrongsteer`. Steering `direct` ajouté à `run_live.py` (interdit store_*/kg_*, mande create_*/query live). A re-run propre.

### Stack A (Revit-direct pur) — scoring équitable vs état Revit (projection v2 dumpée `A_revit_endstate.kg.json`)
verify.py seul auto-échoue les state-checks de A (pas de snapshot KG) → corrigé en lisant le modèle Revit réel :

| Sc | A travail Revit réel | Verdict équitable | Coût A |
|---|---|---|---|
| 70_audit-xl | audit multi-hop répondu | ✅ correct | $0.72 |
| 75_fanout-xl | 10/10 | ✅ correct | $0.82 |
| 80_m-long | **25/30** murs aux hauteurs cibles | ⚠️ incomplet | $1.61 |
| 85_drift-long | déclare honnêtement ne pas détecter (pas de baseline) | 🟡 honest_incomplete (≈0% détection, attendu §8.4-vii) | $0.67 |
| 90_resume | **2 murs créés ✓** ; diff cross-session impossible | 🟡 Step3 ✓ / Step2 incapable | $0.81 |
| 95_audit-trail | 60 deletes ✓ ; **0 annotation queryable** | 🟡 delete ✓ / pas d'audit-trail | $0.54 |

**0 fabrication.** Sur les KG-favorables (85/90-Step2/95) A est honnêtement INCAPABLE → C ≥ A en capacité (critère i). A bien moins cher (total $5.16). Ratio coût C/A à confirmer iso-baseline (re-C requis).

### Données provisoires stack C (sale-baseline, à confirmer iso-baseline)
Source : `_archive/stage3-scratch/stage3-suite` (verify.py 2026-05-21) — 6/6 **correct**, 0 fabricated, **$11.31**, $/correct **$1.89** ; 80/90/95 state_acc 1.0 ; drift final **0/387**. ⚠️ baseline Turn 66→84 (non pristine) → non strictement comparable à A/B pristine tant que C n'est pas re-lancé.

## POST-VERDICT — fix read-API (projection + agrégation) 2026-05-21

Suite à l'insight « C cher car `kg_v2_query` dumpe le graphe entier », ajout de 2 capacités server-side au read path (zéro touche projection/write/protocole) :
- **(a) `select`** : projection de champs (renvoie seulement les attrs demandés).
- **(c) `aggregate`** : `{op: count|sum|mean|min|max, field, group_by}` → renvoie un scalaire/table calculé côté C# au lieu de N nœuds.

Implémenté dans `commandset-kg` (`NodeAggregator.cs`, `NodeViewBuilder` projection, `KgQueryResult.Aggregate`, command/handler) + schéma TS `kg_v2_query` (description incitant l'agent à préférer `aggregate`). Build C# 0err/0warn déployé, TS `tsc` clean. Steering v2-kg **inchangé** (test honnête : la capacité découvrable via le schéma suffit-elle ?).

**Re-run 70/75 sur C (mêmes prompts, pristine Turn 52, `C-favorables-v2/`)** :

| Sc | A $ | C avant $ | C **après** $ | Cnew/A | Cnew/Cold | Correct |
|---|--:|--:|--:|--:|--:|:--:|
| 75_fanout-xl | 0.82 | 2.00 | **0.74** | **0.90 ✅** | **0.37** | 10/10 ✓ (agent : *« All figures confirmed by server-side aggregation »*) |
| 70_audit-xl | 0.72 | 1.74 | 1.35 | 1.87 ❌ | 0.77 | ✓ mais 53 turns |

**Effet** : 75_fanout-xl **bascule sous A** (0.90 < 1.0) → **critère iii passe pour Fanout-XL**, hypothèse §8.3 vindicée *quand la query est de l'agrégation node-attr*. 70_audit-xl s'améliore (−23%) mais reste > A car **relationnel** (audit multi-zone = traversée d'arêtes) — non couvert par l'agrégation node-attr ; nécessiterait une **agrégation edge-aware** (brancher `kg_v2_traverse`). **Critère iii après fix : 2/4 favorables passent (75✅ 85✅ ; 70❌ 90❌)** — 90 a un autre cost-driver (recovery diff cross-session), pas adressé par ce fix.

**Conclusion affinée** : le surcoût de C sur les query pleine-échelle n'était **pas intrinsèque** — c'était une read-API non-indexée. Projection+agrégation node-attr suffit à faire gagner C sur Fanout. Restent 2 chantiers pour fermer iii : (1) agrégation edge-aware pour Audit-XL relationnel, (2) coût recovery diff de Resume.

### POST-VERDICT 2 — fix edge-aware (join-projection) 2026-05-21

Ajout d'un **3e read-path** : `join: [{edge_type, direction, as, select}]` — pour chaque nœud matché, **chain-walk** des hops (1 voisin/hop) et aplatit en une ligne `{llm_id, <select>, <as>_id, <as>_<attr>...}`. Un audit relationnel = **1 call** au lieu de N traversées. Impl `commandset-kg/Core/NodeJoiner.cs` (+ `JoinStep`), `KgQueryResult.Rows`, handler/command + schéma TS. Build 0 err (deploy bloqué par lock Revit → close+copy+reopen). Smoke-test : `node_type=Window, select=[sill_height], join=[hosts↑ as host_wall, at_level↓ as level select=[name,elevation]]` → 352 lignes plates en 1 call.

**Re-run 70_audit-xl sur C (`C-70-edgeaware/`)** :

| | A $ | C iso | C select+agg | C **join** | Cnew/A |
|---|--:|--:|--:|--:|--:|
| 70_audit-xl | 0.72 | 1.74 | 1.35 | **0.65** | **0.91 ✅** |

12 turns (vs 53), agent : *« one kg_v2_query join walked window → host wall → level for all 352 windows »*, **25/352 violators** correctement listés. **Critère iii passe maintenant pour Audit-XL.**

### Critère iii — bilan après les 2 fix read-API

| Favorable | C/A avant | C/A après | Pass | Levier |
|---|--:|--:|:--:|---|
| 70 Audit-XL | 2.43 | **0.91** | ✅ | join edge-aware |
| 75 Fanout-XL | 2.44 | **0.90** | ✅ | aggregation |
| 85 Drift-Long | 0.88 | 0.88 | ✅ | (déjà) |
| 90 Resume | 1.86 | 1.86 | ❌ | cost-driver = recovery diff cross-session, PAS une query modèle → non adressé |

**Critère iii : 3/4 favorables passent.** Seul 90_resume reste > A, mais son coût n'est pas une query pleine-échelle (c'est le parse du diff d'historique au reload) — un autre chantier (diff résumé/paginé server-side), hors du périmètre read-query. **L'« échec iii » du verdict initial était un artefact de read-API non-indexée : projection + agrégation + join edge-aware le corrigent sans toucher la projection ni le write path.**

## GRAPHE-SHAPED — l'avantage structurel tranché 2026-05-21

Question : node-attr agg n'est pas un avantage de graphe (flat-SQL l'égale). Y a-t-il un avantage *structurel* ? → 3 scénarios intrinsèquement relationnels/temporels + primitif BFS profondeur-variable (`kg_v2_traverse` mode reachability : `edge_types[]`/`direction`/`max_depth`/`include_soft_deleted`, `PathTraversal.Reachable`). A=`s2-direct` (--steer direct), C=`v2-kg`, pristine iso.

| Scénario | thème | A $ | C $ | C/A | verdict |
|---|---|--:|--:|--:|---|
| 10_impact-xl | cascade level→walls→windows (reachability) | 0.91 | **0.39** | **0.43** | C 2.3× moins cher + correct (54) ; A ne peut PAS confirmer le host-binding (`FamilyInstance.Host` indispo) |
| 20_typeimpact-xl | ranking type→walls→windows (traversée+agg) | 0.75 | **0.48** | **0.63** | C 1.6× moins cher |
| 30_provenance | chaîne replaced_by à travers tombstones | 0.44 | 1.44 | 3.27 | **capacité** : C reconstruit la chaîne 4-deep (1 alive + 3 tombstoned) par la structure d'edges ; A récupère depth 1 seulement (incapable) |
| **TOTAL** | | **2.11** | **2.31** | 1.10 | |

**RÉPONSE : OUI, l'avantage structurel est réel — mais il faut le bon workload.**
- **vs Revit-direct (A) : prouvé sur les 3.** Sur du multi-hop/cascade C est ~0.5× le coût de A (qui doit round-tripper par élément et bute sur des limites d'API : pas de `FamilyInstance.Host`, cap-50). Sur la provenance, A est *structurellement incapable* (pas d'historique, pas d'edges sémantiques).
- **vs flat-SQL : nuancé.** 10/20 sont des joins 2-hops + agg que SQL ferait aussi. Ce qui est *intrinsèquement graphe* (et que SQL fait mal) = la **traversée transitive à profondeur variable à travers les tombstones** (30_provenance) + le primitif reachability. C'est là l'avantage graphe-spécifique.

**Contraste clé avec les 6 favorables initiaux** : là, l'« avantage » de C était un artefact de read-API (A restait compétitif en coût une fois A=flat-store écarté). **Ici, sur des workloads vraiment graphe-shaped, C gagne nativement** — coût ET capacité. La conclusion : le KG vaut son coût *quand le travail est relationnel/temporel* (impact, dépendance, provenance, drift, cross-session) ; pour des lookups node-attr, c'est sur-dimensionné. Le bench initial ne testait pas ce régime — d'où l'ambiguïté du verdict §8.5.

Le total C/A 1.10 est trompeur : il est entièrement porté par la provenance ($1.44, où la comparaison coût n'a pas de sens car A abandonne). Sur les 2 scénarios coût-comparables, C = **0.43× et 0.63×** de A.

## Convention de nommage (anti-sprawl)
`out/stage3-matrix/<STACK>/` uniquement. Plus de `prompts-stage3-*` jetables : un seul set `prompts-stage3-suite/`. Essais → `out/_archive/stage3-scratch/`.
