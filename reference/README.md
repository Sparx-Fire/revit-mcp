# `reference/` — spec figée du port

La **référence du port TS** (spec : `../DESIGN-internalize-es.md` §7) est :

    kg_bridge/vendor/project_kg.py   + ses 821 tests upstream

Elle **n'est pas dupliquée ici** volontairement : un second exemplaire
divergerait silencieusement de la référence. `kg_bridge/` est conservé
tel quel sur la branche v1 *tant que le port n'est pas vert*, puis retiré
(plan §10, étape 4) — la copie figée sera alors relogée ici avec son
empreinte (hash) pour ancrer la version exacte portée.

En attendant, considérer `kg_bridge/vendor/project_kg.py` comme **lecture
seule** : c'est un contrat, pas du code à modifier.
