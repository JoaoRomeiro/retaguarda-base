# Checksums das bibliotecas de frontend (Retaguarda.Web)

Procedência e integridade das bibliotecas vendoradas localmente em `wwwroot/lib/`,
conforme **seção 15.7** do `docs/legado/orientacao-retaguarda.md`.

## O que este arquivo prova (e o que não prova)

- **Prova:** se algum arquivo aqui listado for modificado depois desta etapa, o hash registrado deixa de bater → você detecta tampering local.
- **Prova:** o ZIP de origem foi baixado via HTTPS do release oficial no GitHub (TLS garante autenticidade do servidor `github.com`). O hash do ZIP está registrado e pode ser re-baixado e comparado.
- **Não prova:** assinatura criptográfica do upstream — nem Inter, nem Bootstrap, nem Bootstrap Icons publicam `.sig`/`.minisig` dos releases. Garantia é só "HTTPS do release oficial + hash local".

## Como reverificar

```powershell
# Verificar hash de um arquivo específico:
Get-FileHash -Algorithm SHA256 src/Retaguarda.Web/wwwroot/lib/inter/InterVariable.woff2

# Listar hash de TUDO coberto neste documento:
$base = "src/Retaguarda.Web/wwwroot/lib"
Get-ChildItem $base -Recurse -File -Exclude CHECKSUMS.md | ForEach-Object {
    "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash, $_.FullName
}
```

---

## Inter v4.1

- **Site oficial:** https://rsms.me/inter/
- **Release oficial:** https://github.com/rsms/inter/releases/tag/v4.1
- **ZIP de origem:** https://github.com/rsms/inter/releases/download/v4.1/Inter-4.1.zip
- **SHA-256 do ZIP oficial:** `9883FDD4A49D4FB66BD8177BA6625EF9A64AA45899767DDE3D36AA425756B11E`
- **Licença:** SIL Open Font License 1.1 (arquivo `LICENSE` incluído)

Formato escolhido: **variable font** (1 arquivo cobrindo todos os pesos de 100 a 900 via CSS `font-weight`).
Italics fora de escopo nesta versão.

| Arquivo | SHA-256 | Tamanho |
|---|---|---|
| `inter/InterVariable.woff2` | `693B77D4F32EE9B8BFC995589B5FAD5E99ADF2832738661F5402F9978429A8E3` | 352.240 bytes |
| `inter/LICENSE` | `262481E844521B326F5ECD053E59B98C8B2DA78C8EE1BDBB6E8174305E54935A` | 4.380 bytes |

---

## Bootstrap Icons v1.13.1

- **Site oficial:** https://icons.getbootstrap.com/
- **Release oficial:** https://github.com/twbs/icons/releases/tag/v1.13.1
- **ZIP de origem:** https://github.com/twbs/icons/releases/download/v1.13.1/bootstrap-icons-1.13.1.zip
- **SHA-256 do ZIP oficial:** `999021E12FAB5C9EDE5E4E7072EB176122BE798B2F99195ACF5DDA47AEF8FC93`
- **LICENSE:** baixado separadamente de `https://raw.githubusercontent.com/twbs/icons/v1.13.1/LICENSE` — o ZIP do release **não inclui** o arquivo de licença.
- **Licença:** MIT

Incorporamos apenas o necessário pra usar a biblioteca como **web font** (`<i class="bi bi-house"></i>`). Os 1900+ SVGs individuais (uso inline-SVG) **não foram incluídos**.

| Arquivo | SHA-256 | Tamanho |
|---|---|---|
| `bootstrap-icons/bootstrap-icons.css` | `004322721C8557331759BC6DDAACBB689B0F0715D688AEC82BD056D2D5B5CC3B` | 99.556 bytes |
| `bootstrap-icons/fonts/bootstrap-icons.woff2` | `6C75710364A1CA5604267716F6D28997B26319FDB078CF11E0B42AB66FF2EA61` | 134.044 bytes |
| `bootstrap-icons/fonts/bootstrap-icons.woff` | `F55513B7B591CB84A3B87FF0E34EA24D4831D6FEDC22E54B911CA64B5B544A15` | 180.288 bytes |
| `bootstrap-icons/LICENSE` | `0FB3E11BD57E896C5A512AFD64864D28A37DE45D19835016C87CA1AD19EAD969` | 1.093 bytes |

---

## Bootstrap v5.3.3

- **Site oficial:** https://getbootstrap.com/
- **Release oficial:** https://github.com/twbs/bootstrap/releases/tag/v5.3.3
- **ZIP de origem:** https://github.com/twbs/bootstrap/releases/download/v5.3.3/bootstrap-5.3.3-dist.zip
- **SHA-256 do ZIP oficial:** `5B0A245AB8458951668D29FD149FC6C8E63C676977EFC23822A223C08245DE1A`
- **LICENSE:** baixado separadamente de `https://raw.githubusercontent.com/twbs/bootstrap/v5.3.3/LICENSE` — o ZIP `-dist` **não inclui** o arquivo de licença.
- **Licença:** MIT

**Histórico (Etapa 1.10.1):** os arquivos originalmente vendorados pelo template `dotnet new mvc` (Etapa 1.2) **não eram bit-a-bit idênticos** ao release oficial — apresentavam tamanhos divergentes (ex.: `bootstrap.css` local = 293.102 bytes vs oficial = 281.046 bytes), provavelmente por cabeçalhos/comentários adicionados pelo template. Foram **substituídos pelos oficiais** nesta etapa pra obter procedência limpa e auditável.

A estrutura de pastas `bootstrap/dist/css/` e `bootstrap/dist/js/` foi preservada porque é referenciada por `Views/Shared/_Layout.cshtml`.

### CSS (`bootstrap/dist/css/`)

| Arquivo | SHA-256 | Tamanho |
|---|---|---|
| `bootstrap/dist/css/bootstrap.css` | `18A105D7CB38E01E5ED0CA255C092992A2E211B39594A7FA57262BFC6FC4EA9C` | 281.046 bytes |
| `bootstrap/dist/css/bootstrap.css.map` | `2B3355477A7B51919B6BDE1D9C2B6573A8D78CAE6EAD23E267F78D9CA4E60E4C` | 679.755 bytes |
| `bootstrap/dist/css/bootstrap.min.css` | `3C8F27E6009CCFD710A905E6DCF12D0EE3C6F2AC7DA05B0572D3E0D12E736FC8` | 232.803 bytes |
| `bootstrap/dist/css/bootstrap.min.css.map` | `F12338536350A422C64D02D6E43FF1DEA493C3156AD823FE19761CDD5D56C05B` | 589.892 bytes |
| `bootstrap/dist/css/bootstrap.rtl.css` | `8F91385C88F5A7590D9C38B4C75B5E5FD457A21D7F14D94FBC230BF589918764` | 280.259 bytes |
| `bootstrap/dist/css/bootstrap.rtl.css.map` | `DDB61652289560C65FBF6C2AE499D722C1E540A8122ACFD57118AF823819D61A` | 679.615 bytes |
| `bootstrap/dist/css/bootstrap.rtl.min.css` | `879944ECD9BC4A4788A411C763137DF6CA4FDD5B8614A97935982CA1C8A5EF39` | 232.911 bytes |
| `bootstrap/dist/css/bootstrap.rtl.min.css.map` | `AD3CD79677A971BFEF80502207E53B3834007CE9492DF337818E8853B78800D9` | 589.087 bytes |
| `bootstrap/dist/css/bootstrap-grid.css` | `632E7F841A919A6536309D532B03F6697A133BAF8E8F3ACB98922C0B65B2E07F` | 70.329 bytes |
| `bootstrap/dist/css/bootstrap-grid.css.map` | `C404FE9F6E4513986F3A38F67C6E1874EC2BD5B97822500904D83A3DB84B4F61` | 203.221 bytes |
| `bootstrap/dist/css/bootstrap-grid.min.css` | `E670C73068B27D91E5DD45DE3EE84B0D047D9DC3DF051D4725E64B5F224D576E` | 51.795 bytes |
| `bootstrap/dist/css/bootstrap-grid.min.css.map` | `9202FEC7056633C20EB35E659E81EDF5D691D8B44C8811BF7188143DC290398E` | 115.986 bytes |
| `bootstrap/dist/css/bootstrap-grid.rtl.css` | `099C6817CCE368B97256472F5420E573C09E411D70D5132BBE0631DF472CF243` | 70.403 bytes |
| `bootstrap/dist/css/bootstrap-grid.rtl.css.map` | `FEC890500F325FCDF48FE70BE1A98A1D8DF206D9F79FCCF7120F95675E5FF749` | 203.225 bytes |
| `bootstrap/dist/css/bootstrap-grid.rtl.min.css` | `BCCC5372F902E0BCBB2E2013DC6F32132F44A53AFB1607B849CCD6A74EFFA779` | 51.870 bytes |
| `bootstrap/dist/css/bootstrap-grid.rtl.min.css.map` | `EC674E970ED4FF083269E52D166C4FCF9FCCA6176F5523ED54E3A54E7F5CDF74` | 116.063 bytes |
| `bootstrap/dist/css/bootstrap-reboot.css` | `968F5823CD8E174DEFA2376EF97391DFE0D1ACB229321A7364D9876CCE420CC0` | 12.065 bytes |
| `bootstrap/dist/css/bootstrap-reboot.css.map` | `45727F4198817C75E83ED5D1D84802F9B168DA97B71AD6D93BBDB646D88B1B34` | 129.371 bytes |
| `bootstrap/dist/css/bootstrap-reboot.min.css` | `97CBEDE68A33BFDE7C78C77D4C5B0F016825F4924AF5829F6D54ACF2672143CE` | 10.126 bytes |
| `bootstrap/dist/css/bootstrap-reboot.min.css.map` | `D1EA954FADA4A912C987DA13A8B7881F852742C92A56389BF219767D7C65E258` | 51.369 bytes |
| `bootstrap/dist/css/bootstrap-reboot.rtl.css` | `57CA6C9C7A094BF30F9425D6C1CFC9DED1ADA7D7378061519AAB08420A67F868` | 12.058 bytes |
| `bootstrap/dist/css/bootstrap-reboot.rtl.css.map` | `3A8415C21EC0AE9EDB5682B66624F1D92FFF2AB9CFAD23F33D9F770AA08C85EF` | 129.386 bytes |
| `bootstrap/dist/css/bootstrap-reboot.rtl.min.css` | `FFC8E1F217043052B24BA8285AA9CDBBBB7713364F0867506603BAB02908F2D2` | 10.198 bytes |
| `bootstrap/dist/css/bootstrap-reboot.rtl.min.css.map` | `F75D33C3EACC75C8342ECE3C013A7AE6F127F2B7791EF3F138A9B6C77FC20482` | 63.943 bytes |
| `bootstrap/dist/css/bootstrap-utilities.css` | `D81B9B80D50F950485FF4C0B15C45743F623CE4F6FB146C301E2B640CFA1FB2A` | 107.823 bytes |
| `bootstrap/dist/css/bootstrap-utilities.css.map` | `35F8EB73852BF45BF6A0112CC10588C819CD0CFF7DABE2E12FECFDE79DCED1C6` | 267.535 bytes |
| `bootstrap/dist/css/bootstrap-utilities.min.css` | `2B213DC5B28EF42B98C741D7A482A0B1622F5E401F213B62435EF68F6EB0991B` | 85.352 bytes |
| `bootstrap/dist/css/bootstrap-utilities.min.css.map` | `AC70E68A9E09673B9A18E71243541242B21B1B411BDD98DAF7086A485D736085` | 180.381 bytes |
| `bootstrap/dist/css/bootstrap-utilities.rtl.css` | `1FAC2405B4B08EE6B6BDE2684E1268E2ECB5EB58E9F833A2653968525719EAA4` | 107.691 bytes |
| `bootstrap/dist/css/bootstrap-utilities.rtl.css.map` | `A74055AB955EFDDA2104875F6EB66CA1036ED36252B0A875834C1BCA242251A5` | 267.476 bytes |
| `bootstrap/dist/css/bootstrap-utilities.rtl.min.css` | `18052E9BA163C10F07AD719AA054671D3A9040BA658D719ABD3EE6057F04F6A5` | 85.281 bytes |
| `bootstrap/dist/css/bootstrap-utilities.rtl.min.css.map` | `A3C5CADF699C63F15F70E4350F61C9BD5B99D184D74944590CB5C22B47E741E0` | 180.217 bytes |

### JS (`bootstrap/dist/js/`)

| Arquivo | SHA-256 | Tamanho |
|---|---|---|
| `bootstrap/dist/js/bootstrap.js` | `F945BCD36C2055F9E36926DDC321CB954EC056995BD164E83A5BCDD429F321A7` | 145.401 bytes |
| `bootstrap/dist/js/bootstrap.js.map` | `F56AFB1F17BC802243A081E1E713F6F65757BC2DE43761A489F423F5CF2F631E` | 306.606 bytes |
| `bootstrap/dist/js/bootstrap.min.js` | `DE040986D9A3ED89D5D5F9AD6D5727015E9E238C2CD13AF8F1B55909386D0864` | 60.635 bytes |
| `bootstrap/dist/js/bootstrap.min.js.map` | `648D357BF9ECE3BDC62AF0021B831C82025DC6F15F148C38C6CA704221BC61EE` | 220.561 bytes |
| `bootstrap/dist/js/bootstrap.bundle.js` | `9A4A11A15DB88D5FAB08F59C1C34796B03F1F15BB3CC928DD226E1C59F7F59A3` | 207.819 bytes |
| `bootstrap/dist/js/bootstrap.bundle.js.map` | `5AAE1A596D6B41D27EEA8020CB525073D2018C72FD4F730A7D74C136A3AFD367` | 444.579 bytes |
| `bootstrap/dist/js/bootstrap.bundle.min.js` | `0833B2E9C3A26C258476C46266E6877FC75218625162E0460BE9A3A098A61C6C` | 80.721 bytes |
| `bootstrap/dist/js/bootstrap.bundle.min.js.map` | `5E3E0763164143BAAA1CA0706B6100BA0452F911D6CE9713B48E3DBE07B35125` | 332.090 bytes |
| `bootstrap/dist/js/bootstrap.esm.js` | `7B189764D243C2E7177EE8DEDC26D73DBB92EBE12BCB7CFDB0FFA9826BE1F270` | 135.829 bytes |
| `bootstrap/dist/js/bootstrap.esm.js.map` | `10F44B829A9691A84BC449FA0948DD33BE91218230D71747C136DE1ECB2E8FFE` | 305.438 bytes |
| `bootstrap/dist/js/bootstrap.esm.min.js` | `4197454F564D765CB8AE681406D5E65C54BD054D454DAFAC3DEEA1EFDE2C1514` | 73.935 bytes |
| `bootstrap/dist/js/bootstrap.esm.min.js.map` | `4EC6EFF33E9594D815113F31BF3FF22E8FEFE622474C08F62788648633F5AC73` | 222.455 bytes |

### LICENSE

| Arquivo | SHA-256 | Tamanho |
|---|---|---|
| `bootstrap/LICENSE` | `8C14611AE41AC6FD543C13349F22188EB12C69B3E59105C5ECA3925A8E4ECA3E` | 1.093 bytes |

---

## Histórico de remoções (Etapa 1.10.1)

As bibliotecas abaixo, vendoradas pelo template `dotnet new mvc` (Etapa 1.2), **foram removidas** porque conflitavam com o princípio §15.5 do doc ("JS é tempero, vanilla, nunca arquitetura"):

| Pasta removida | Versão | Razão |
|---|---|---|
| `jquery/` (3.7.1) | 3.7.1 | Não listada na §15.6. Estava aqui só pra suportar `jquery.validate.unobtrusive`. Bootstrap 5 não precisa de jQuery. |
| `jquery-validation/` | 1.21.0 | Idem. |
| `jquery-validation-unobtrusive/` | 4.0.0 | Idem. |

**Decisão substitutiva:** validação client-side será implementada em **vanilla JS** dentro de `forms.js` quando o primeiro formulário relevante demandar UX inline. Até lá, validação roda **server-side** (sempre presente via Data Annotations + ASP.NET Core). Estratégia alinhada com §15.5 e §15.6 do doc.

**Mudanças associadas no código:**
- `Views/Shared/_Layout.cshtml`: removida tag `<script src="~/lib/jquery/dist/jquery.min.js"></script>`.
- `Views/Shared/_ValidationScriptsPartial.cshtml`: arquivo deletado (não era referenciado por nenhuma view ativa).
