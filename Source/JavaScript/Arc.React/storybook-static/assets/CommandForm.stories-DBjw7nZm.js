var fe=Object.defineProperty;var ve=(e,t,n)=>t in e?fe(e,t,{enumerable:!0,configurable:!0,writable:!0,value:n}):e[t]=n;var f=(e,t,n)=>ve(e,typeof t!="symbol"?t+"":t,n);import{j as r}from"./jsx-runtime-Cf8x2fCZ.js";import{r as h,R as y}from"./index-BO-NyGGJ.js";import{C as ye,G as Ce,a as de,P as j,b as me}from"./PropertyDescriptor-Bo5Iu_vo.js";import{S as R,c as ce}from"./StoryContainer-B2MOqGRO.js";import"./index-yBjzXJbu.js";const ue=h.createContext(void 0),he=()=>{const e=h.useContext(ue);if(!e)throw new Error("useCommandFormContext must be used within a CommandForm");return e},D=({field:e})=>{var x;const t=he(),n=e.props,d=n.value,o=d?W(d):"",s=y.useMemo(()=>{var c;return o?(c=t.commandInstance)==null?void 0:c[o]:void 0},[t.commandInstance,o,t.commandVersion]),l=o?t.getFieldError(o):void 0,a=o&&((x=t.commandInstance)!=null&&x.propertyDescriptors)?t.commandInstance.propertyDescriptors.find(c=>c.name===o):void 0,p=y.cloneElement(e,{...n,currentValue:s,propertyDescriptor:a,fieldName:o,onValueChange:c=>{var m;if(o){const u=s;if(t.setCommandValues({[o]:c}),t.commandInstance&&typeof t.commandInstance.validate=="function"){const v=t.commandInstance.validate();v&&t.setCommandResult(v)}if(t.onFieldValidate){const v=t.onFieldValidate(t.commandInstance,o,u,c);t.setCustomFieldError(o,v)}t.onFieldChange&&t.onFieldChange(t.commandInstance,o,u,c)}(m=n.onChange)==null||m.call(n,c)},required:n.required??!0,invalid:!!l}),g=t.fieldContainerComponent,i=r.jsxs(r.Fragment,{children:[t.showTitles&&n.title&&r.jsx("label",{style:{display:"block",marginBottom:"0.5rem",fontWeight:500,color:"var(--color-text)"},children:n.title}),r.jsxs("div",{style:{display:"flex",width:"100%"},children:[n.icon&&r.jsx("span",{title:n.description,style:{cursor:n.description?"help":"default",display:"flex",alignItems:"center",padding:"0.5rem",backgroundColor:"var(--color-background-secondary)",border:"1px solid var(--color-border)",borderRight:"none",borderRadius:"var(--radius-md) 0 0 var(--radius-md)"},children:n.icon}),p]}),t.showErrors&&l&&r.jsx("small",{style:{display:"block",marginTop:"0.25rem",color:"var(--color-error, #c00)",fontSize:"0.875rem"},children:l})]});return g?r.jsx(g,{title:n.title,errorMessage:l,children:i}):r.jsx("div",{className:"w-full",style:{marginBottom:"1rem"},children:i})};D.displayName="CommandFormFieldWrapper";const pe=e=>{const{fields:t,columns:n,orderedChildren:d}=e;return n&&n.length>0?r.jsx("div",{className:"card flex flex-column md:flex-row gap-3",children:n.map((o,s)=>r.jsx("div",{className:"flex flex-column gap-3 flex-1",children:o.fields.map((l,a)=>{const g=l.props.value,i=g?W(g):`field-${s}-${a}`;return r.jsx(D,{field:l},i)})},`column-${s}`))}):d&&d.length>0?r.jsx("div",{style:{display:"flex",flexDirection:"column",width:"100%"},children:d.map(o=>{if(o.type==="field"){const s=o.content,a=s.props.value,p=a?W(a):`field-${o.index}`;return r.jsx(D,{field:s},p)}else return r.jsx(y.Fragment,{children:o.content},`other-${o.index}`)})}):r.jsx("div",{style:{display:"flex",flexDirection:"column",width:"100%"},children:(t||[]).map((o,s)=>{const a=o.props.value,p=a?W(a):`field-${s}`;return r.jsx(D,{field:o},p)})})};function W(e){const n=e.toString().match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);return n?n[1]:""}pe.displayName="CommandFormFields";const be={addCommand:()=>{},execute:async()=>new ye(new Map),hasChanges:!1,revertChanges:()=>{}},je=y.createContext(be),Fe=y.createContext({microservice:Ce.microservice,development:!1,origin:"",basePath:"",apiBasePath:"",httpHeadersCallback:()=>({})});function ge(e,t){var c;const n=h.useRef(null),[d,o]=h.useState(!1),[s,l]=h.useState(0),a=h.useContext(Fe),p=h.useCallback(()=>{var m,u;((m=n.current)==null?void 0:m.hasChanges)!==d&&o(((u=n.current)==null?void 0:u.hasChanges)??!1)},[]);n.current=h.useMemo(()=>{const m=new e;return m.setMicroservice(a.microservice),m.setApiBasePath(a.apiBasePath??""),m.setOrigin(a.origin??""),m.setHttpHeadersCallback(a.httpHeadersCallback??(()=>({}))),t&&m.setInitialValues(t),m.onPropertyChanged(p,m),m},[]);const g=y.useContext(je);(c=g.addCommand)==null||c.call(g,n.current);const i=h.useCallback(m=>{const u=m;n.current.propertyDescriptors.forEach(v=>{u[v.name]!==void 0&&u[v.name]!=null&&(n.current[v.name]=u[v.name])}),l(v=>v+1)},[]),x=h.useCallback(()=>{n.current.propertyDescriptors.forEach(m=>{n.current[m.name]=void 0}),l(m=>m+1)},[]);return[n.current,i,x,s]}function Se(e){if(typeof e!="function")return"";const n=e.toString().match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);return n?n[1]:""}const O=e=>(y.Children.forEach(e.children,t=>{if(y.isValidElement(t)){const n=t.type;if(n.displayName!=="CommandFormField")throw new Error(`Only CommandFormField components are allowed as children of CommandForm.Fields. Got: ${n.displayName||n.name||"Unknown"}`)}}),r.jsx(r.Fragment,{}));O.displayName="CommandFormFieldsWrapper";const we=e=>{if(!e.children)return{fieldsOrColumns:[],otherChildren:[],initialValuesFromFields:{},orderedChildren:[]};let t=[];const n=[];let d=!1;const o=[],s=[];let l=0,a=0,p={};const g=i=>{const x=i.props;if(x.currentValue!==void 0&&x.value){const c=x.value,m=Se(c);m&&(p={...p,[m]:x.currentValue})}};return y.Children.toArray(e.children).forEach(i=>{if(!y.isValidElement(i)){o.push(i),s.push({type:"other",content:i,index:a++});return}const x=i.type;if(x.displayName==="CommandFormColumn"){d=!0;const c=i.props,m=y.Children.toArray(c.children).filter(u=>y.isValidElement(u)&&u.type.displayName==="CommandFormField"?(g(u),!0):!1);n.push({fields:m})}else if(x.displayName==="CommandFormField")g(i),t.push(i),s.push({type:"field",content:i,index:l++});else if(x===O||x.displayName==="CommandFormFieldsWrapper"){const c=i.props,m=y.Children.toArray(c.children).filter(u=>y.isValidElement(u)&&u.type.displayName==="CommandFormField"?(g(u),!0):!1);t=[...t,...m]}else o.push(i),s.push({type:"other",content:i,index:a++})}),{fieldsOrColumns:d?n:t,otherChildren:o,initialValuesFromFields:p,orderedChildren:s}},M=e=>{const{fieldsOrColumns:t,initialValuesFromFields:n,orderedChildren:d}=h.useMemo(()=>we(e),[e.children]),o=h.useMemo(()=>{if(!e.currentValues)return{};const F=new e.command().properties||[],S={};return F.forEach(_=>{e.currentValues[_]!==void 0&&(S[_]=e.currentValues[_])}),S},[e.currentValues,e.command]),s=h.useMemo(()=>({...o,...n,...e.initialValues}),[o,n,e.initialValues]),l=ge(e.command,s),a=l[0],p=l[1],g=l[3],[i,x]=h.useState(void 0),[c,m]=h.useState({}),[u,v]=h.useState({}),V=y.useRef(!1);y.useEffect(()=>{!V.current&&s&&Object.keys(s).length>0&&(p(s),V.current=!0)},[s]);const w=Object.values(c).every(b=>b),L=h.useCallback((b,F)=>{m(S=>({...S,[b]:F}))},[]),H=h.useCallback((b,F)=>{v(S=>{if(F===void 0){const _={...S};return delete _[b],_}return{...S,[b]:F}})},[]),Z=b=>{if(u[b])return u[b];if(!(!i||!i.validationResults)){for(const F of i.validationResults)if(F.members&&F.members.includes(b))return F.message}},N=(i==null?void 0:i.exceptionMessages)||[],I=t.length>0&&"fields"in t[0],$={command:e.command,commandInstance:a,commandVersion:g,setCommandValues:p,commandResult:i,setCommandResult:x,getFieldError:Z,isValid:w,setFieldValidity:L,onFieldValidate:e.onFieldValidate,onFieldChange:e.onFieldChange,onBeforeExecute:e.onBeforeExecute,customFieldErrors:u,setCustomFieldError:H,showTitles:e.showTitles??!0,showErrors:e.showErrors??!0,fieldContainerComponent:e.fieldContainerComponent};return r.jsxs(ue.Provider,{value:$,children:[r.jsx(pe,{fields:I?void 0:t,columns:I?t:void 0,orderedChildren:d}),N.length>0&&r.jsxs("div",{style:{marginTop:"1rem",padding:"1rem",border:"1px solid var(--color-border)",borderRadius:"var(--radius-md)",backgroundColor:"var(--color-error-bg, #fee)"},children:[r.jsx("h4",{style:{margin:"0 0 0.5rem 0",fontSize:"1rem",fontWeight:600,color:"var(--color-error, #c00)"},children:"The server responded with"}),r.jsx("ul",{style:{margin:0,paddingLeft:"1.5rem"},children:N.map((b,F)=>r.jsx("li",{children:b},F))})]})]})},xe=e=>r.jsx(r.Fragment,{});xe.displayName="CommandFormColumn";M.Fields=O;M.Column=xe;const E=M;function T(e,t){var l;const{defaultValue:n,extractValue:d}=t,o=(typeof e=="function"&&!((l=e.prototype)!=null&&l.render),e),s=a=>{const{currentValue:p,onValueChange:g,fieldName:i,required:x=!0,...c}=a,{getFieldError:m,customFieldErrors:u}=he(),v=i?m(i):void 0,V=i?u[i]:void 0,w=[];v&&w.push(v),V&&w.push(V);const L=w.length>0,N={...c,value:p!==void 0?p:n,onChange:I=>{const $=d?d(I):I;g==null||g($)},invalid:L,required:x,errors:w};return r.jsx(o,{...N})};return s.displayName="CommandFormField",s}const C=T(e=>r.jsx("input",{type:e.type||"text",value:e.value,onChange:e.onChange,required:e.required,placeholder:e.placeholder,className:`w-full p-3 rounded-md text-base ${e.invalid?"border border-red-500":"border border-gray-300"}`,style:{width:"100%",display:"block"}}),{defaultValue:"",extractValue:e=>e&&typeof e=="object"&&"target"in e?e.target.value:String(e||"")}),_e=T(e=>r.jsx("input",{type:"number",value:e.value,onChange:e.onChange,required:e.required,placeholder:e.placeholder,min:e.min,max:e.max,step:e.step,className:`w-full p-3 rounded-md text-base ${e.invalid?"border border-red-500":"border border-gray-300"}`,style:{width:"100%",display:"block"}}),{defaultValue:0,extractValue:e=>e&&typeof e=="object"&&"target"in e?parseFloat(e.target.value)||0:typeof e=="number"?e:0}),Ee=T(e=>r.jsxs("div",{className:"flex items-center",children:[r.jsx("input",{type:"checkbox",checked:e.value,onChange:e.onChange,required:e.required,className:`h-5 w-5 rounded ${e.invalid?"border-red-500":"border-gray-300"}`}),e.label&&r.jsx("label",{className:"ml-2",children:e.label})]}),{defaultValue:!1,extractValue:e=>typeof e=="boolean"?e:e&&typeof e=="object"&&"target"in e?e.target.checked:!1}),Te=T(e=>r.jsx("textarea",{value:e.value,onChange:e.onChange,required:e.required,placeholder:e.placeholder,rows:e.rows??5,cols:e.cols,className:`w-full p-3 rounded-md text-base ${e.invalid?"border border-red-500":"border border-gray-300"}`,style:{width:"100%",display:"block"}}),{defaultValue:"",extractValue:e=>e&&typeof e=="object"&&"target"in e?e.target.value:String(e||"")}),Ve=e=>r.jsxs("select",{value:e.value||"",onChange:e.onChange,required:e.required,className:`w-full p-3 rounded-md text-base ${e.invalid?"border border-red-500":"border border-gray-300"}`,style:{width:"100%",display:"block"},children:[e.placeholder&&r.jsx("option",{value:"",children:e.placeholder}),e.options.map((t,n)=>r.jsx("option",{value:String(t[e.optionIdField]),children:String(t[e.optionLabelField])},n))]}),Ie=T(Ve,{defaultValue:"",extractValue:e=>e&&typeof e=="object"&&"target"in e?e.target.value:String(e)}),Re=T(e=>{const t=e.min??0,n=e.max??100,d=e.step??1;return r.jsxs("div",{className:"w-full flex items-center gap-4 p-3 border border-gray-300 rounded-md",style:{display:"flex",alignItems:"center",gap:"1rem",padding:"0.75rem",border:"1px solid var(--color-border)",borderRadius:"0.375rem",backgroundColor:"var(--color-background-secondary)"},children:[r.jsx("input",{type:"range",value:e.value,onChange:e.onChange,min:t,max:n,step:d,required:e.required,className:"flex-1",style:{flex:1}}),r.jsx("span",{className:"min-w-[3rem] text-right font-semibold",style:{minWidth:"3rem",textAlign:"right",fontWeight:600,color:"var(--color-text)"},children:e.value})]})},{defaultValue:0,extractValue:e=>e&&typeof e=="object"&&"target"in e?parseFloat(e.target.value):typeof e=="number"?e:typeof e=="string"?parseFloat(e):0});class Ne extends me{constructor(){super(),this.ruleFor(t=>t.username).notEmpty().minLength(3).maxLength(20),this.ruleFor(t=>t.email).notEmpty().emailAddress(),this.ruleFor(t=>t.password).notEmpty().minLength(8),this.ruleFor(t=>t.age).greaterThanOrEqual(13).lessThanOrEqual(120)}}class U extends de{constructor(){super(Object,!1);f(this,"route","/api/users/register");f(this,"validation",new Ne);f(this,"propertyDescriptors",[new j("username",String),new j("email",String),new j("password",String),new j("confirmPassword",String),new j("age",Number),new j("bio",String),new j("favoriteColor",String),new j("birthDate",String),new j("agreeToTerms",Boolean),new j("experienceLevel",Number),new j("role",String)]);f(this,"_username");f(this,"_email");f(this,"_password");f(this,"_confirmPassword");f(this,"_age");f(this,"_bio");f(this,"_favoriteColor");f(this,"_birthDate");f(this,"_agreeToTerms");f(this,"_experienceLevel");f(this,"_role")}get requestParameters(){return[]}get properties(){return["username","email","password","confirmPassword","age","bio","favoriteColor","birthDate","agreeToTerms","experienceLevel","role"]}get username(){return this._username}set username(n){this._username=n,this.propertyChanged("username")}get email(){return this._email}set email(n){this._email=n,this.propertyChanged("email")}get password(){return this._password}set password(n){this._password=n,this.propertyChanged("password")}get confirmPassword(){return this._confirmPassword}set confirmPassword(n){this._confirmPassword=n,this.propertyChanged("confirmPassword")}get age(){return this._age}set age(n){this._age=n,this.propertyChanged("age")}get bio(){return this._bio}set bio(n){this._bio=n,this.propertyChanged("bio")}get favoriteColor(){return this._favoriteColor}set favoriteColor(n){this._favoriteColor=n,this.propertyChanged("favoriteColor")}get birthDate(){return this._birthDate}set birthDate(n){this._birthDate=n,this.propertyChanged("birthDate")}get agreeToTerms(){return this._agreeToTerms}set agreeToTerms(n){this._agreeToTerms=n,this.propertyChanged("agreeToTerms")}get experienceLevel(){return this._experienceLevel}set experienceLevel(n){this._experienceLevel=n,this.propertyChanged("experienceLevel")}get role(){return this._role}set role(n){this._role=n,this.propertyChanged("role")}static use(n){return ge(U,n)}}const Le={title:"CommandForm/CommandForm",component:E};class q extends de{constructor(){super(Object,!1);f(this,"route","/api/simple");f(this,"validation",new ke);f(this,"propertyDescriptors",[new j("name",String),new j("email",String)]);f(this,"name","");f(this,"email","")}get requestParameters(){return[]}get properties(){return["name","email"]}}class ke extends me{constructor(){super(),this.ruleFor(t=>t.name).notEmpty().minLength(3),this.ruleFor(t=>t.email).notEmpty().emailAddress()}}const Pe=[{id:"user",name:"User"},{id:"admin",name:"Administrator"},{id:"moderator",name:"Moderator"}],k={render:()=>{const[e,t]=h.useState({errors:{},canSubmit:!1});return r.jsxs(R,{size:"sm",asCard:!0,children:[r.jsx("h2",{children:"Simple Command Form with Validation"}),r.jsx("p",{children:"This form demonstrates validation on blur. Fields are validated when you leave them."}),r.jsxs(E,{command:q,initialValues:{name:"",email:""},onFieldChange:async(n,d)=>{const o=await n.validate();if(o.isValid)t(s=>{const{[d]:l,...a}=s.errors;return{errors:a,canSubmit:!0}});else{const s=o.validationResults.find(l=>l.members.includes(d));s&&t(l=>({errors:{...l.errors,[d]:s.message},canSubmit:!1}))}},children:[r.jsx(C,{value:n=>n.name,title:"Name",placeholder:"Enter your name (min 3 chars)"}),e.errors.name&&r.jsx("div",{style:{color:"var(--color-error)",fontSize:"0.875rem",marginTop:"0.25rem",marginBottom:"1rem"},children:e.errors.name}),r.jsx(C,{value:n=>n.email,title:"Email",type:"email",placeholder:"Enter your email"}),e.errors.email&&r.jsx("div",{style:{color:"var(--color-error)",fontSize:"0.875rem",marginTop:"0.25rem",marginBottom:"1rem"},children:e.errors.email}),r.jsxs("div",{style:{marginTop:"1.5rem",display:"flex",gap:"0.5rem",alignItems:"center",flexWrap:"wrap"},children:[r.jsx("button",{type:"submit",disabled:!e.canSubmit,children:"Submit"}),!e.canSubmit&&Object.keys(e.errors).length>0&&r.jsx(ce,{variant:"warning",children:"Please fix validation errors"})]})]})]})}},P={render:()=>{const[e,t]=h.useState(""),[n,d]=h.useState(!1),[o,s]=h.useState([]),l=()=>{t("Form submitted successfully!")};return r.jsxs(R,{size:"sm",asCard:!0,children:[r.jsx("h1",{children:"User Registration Form"}),r.jsx("p",{children:"This form validates progressively as you type. The submit button is enabled only when all validation passes."}),r.jsxs(E,{command:U,initialValues:{username:"",email:"",password:"",confirmPassword:"",age:18,bio:"",favoriteColor:"#3b82f6",birthDate:"",agreeToTerms:!1,experienceLevel:50,role:""},onFieldChange:async(a,p,g,i)=>{console.log(`Field ${p} changed from`,g,"to",i);const x=await a.validate();x.isValid?(s([]),d(!0)):(s(x.validationResults.map(c=>c.message)),d(!1))},children:[r.jsx("h3",{children:"Account Information"}),r.jsx(C,{value:a=>a.username,title:"Username",placeholder:"Enter username"}),r.jsx(C,{value:a=>a.email,title:"Email Address",type:"email",placeholder:"Enter email"}),r.jsx(C,{value:a=>a.password,title:"Password",type:"password",placeholder:"Enter password"}),r.jsx(C,{value:a=>a.confirmPassword,title:"Confirm Password",type:"password",placeholder:"Confirm password"}),r.jsx("h3",{style:{marginTop:"var(--space-2xl)",marginBottom:0},children:"Personal Information"}),r.jsx(_e,{value:a=>a.age,title:"Age",placeholder:"Enter age",min:13,max:120}),r.jsx(C,{value:a=>a.birthDate,title:"Birth Date",type:"date",placeholder:"Select birth date"}),r.jsx(Te,{value:a=>a.bio,title:"Bio",placeholder:"Tell us about yourself",rows:4,required:!1}),r.jsx(C,{value:a=>a.favoriteColor,title:"Favorite Color",type:"color"}),r.jsx("h3",{style:{marginTop:"var(--space-2xl)",marginBottom:0},children:"Preferences"}),r.jsx(Ie,{value:a=>a.role,title:"Role",options:Pe,optionIdField:"id",optionLabelField:"name",placeholder:"Select a role"}),r.jsx(Re,{value:a=>a.experienceLevel,title:"Experience Level",min:0,max:100,step:10}),r.jsx(Ee,{value:a=>a.agreeToTerms,label:"I agree to the terms and conditions"})]}),o.length>0&&r.jsxs("div",{className:"story-card",style:{backgroundColor:"rgba(245, 158, 11, 0.1)",borderColor:"var(--color-warning)",marginBottom:"var(--space-lg)"},children:[r.jsx("strong",{style:{color:"var(--color-warning)"},children:"Validation Issues:"}),r.jsx("ul",{style:{marginTop:"var(--space-sm)",marginBottom:0},children:o.map((a,p)=>r.jsx("li",{style:{color:"var(--color-warning)"},children:a},p))})]}),r.jsxs("div",{style:{display:"flex",gap:"var(--space-md)",marginTop:"var(--space-xl)",alignItems:"center",flexWrap:"wrap"},children:[r.jsx("button",{onClick:l,disabled:!n,style:{backgroundColor:n?"var(--color-success)":void 0},children:"Submit"}),r.jsx("button",{onClick:()=>t(""),style:{backgroundColor:"var(--color-text-muted)"},children:"Cancel"}),!n&&r.jsx(ce,{variant:"warning",children:"Complete required fields with valid data"})]}),e&&r.jsx("div",{className:"story-card",style:{backgroundColor:"rgba(34, 197, 94, 0.1)",borderColor:"var(--color-success)",marginTop:"var(--space-lg)"},children:r.jsx("p",{style:{color:"var(--color-success)",fontWeight:600,margin:0},children:e})})]})}},B={render:()=>r.jsxs(R,{size:"sm",asCard:!0,children:[r.jsx("h2",{children:"Custom Titles"}),r.jsx("p",{children:"This form shows how to disable built-in titles and use custom title rendering."}),r.jsxs(E,{command:q,showTitles:!1,children:[r.jsxs("div",{style:{marginBottom:"1rem"},children:[r.jsx("div",{style:{fontSize:"0.75rem",textTransform:"uppercase",letterSpacing:"0.05em",marginBottom:"0.5rem",color:"var(--color-text-secondary)",fontWeight:600},children:"Full Name *"}),r.jsx(C,{value:e=>e.name,placeholder:"Enter your full name"})]}),r.jsxs("div",{style:{marginBottom:"1rem"},children:[r.jsx("div",{style:{fontSize:"0.875rem",marginBottom:"0.5rem",color:"var(--color-primary)",fontWeight:700},children:"📧 Email Address"}),r.jsx(C,{value:e=>e.email,type:"email",placeholder:"your.email@example.com"})]}),r.jsx("button",{type:"submit",children:"Submit"})]})]})},z={render:()=>{const[e,t]=h.useState({});return r.jsxs(R,{size:"sm",asCard:!0,children:[r.jsx("h2",{children:"Custom Error Rendering"}),r.jsx("p",{children:"This form shows how to disable built-in error messages and render custom ones."}),r.jsxs(E,{command:q,showErrors:!1,onFieldChange:async(n,d)=>{const o=await n.validate();if(o.isValid)t(s=>{const{[d]:l,...a}=s;return a});else{const s=o.validationResults.find(l=>l.members.includes(d));s&&t(l=>({...l,[d]:s.message}))}},children:[r.jsx(C,{value:n=>n.name,title:"Name",placeholder:"Enter your name (min 3 chars)"}),e.name&&r.jsxs("div",{style:{backgroundColor:"rgba(239, 68, 68, 0.1)",border:"1px solid var(--color-error)",borderRadius:"var(--radius-md)",padding:"0.75rem",marginTop:"0.5rem",marginBottom:"1rem",display:"flex",alignItems:"center",gap:"0.5rem"},children:[r.jsx("span",{style:{fontSize:"1.25rem"},children:"⚠️"}),r.jsxs("div",{children:[r.jsx("strong",{style:{color:"var(--color-error)"},children:"Validation Error"}),r.jsx("div",{style:{fontSize:"0.875rem",marginTop:"0.25rem",color:"var(--color-text)"},children:e.name})]})]}),r.jsx(C,{value:n=>n.email,title:"Email",type:"email",placeholder:"Enter your email"}),e.email&&r.jsxs("div",{style:{backgroundColor:"rgba(239, 68, 68, 0.1)",border:"1px solid var(--color-error)",borderRadius:"var(--radius-md)",padding:"0.75rem",marginTop:"0.5rem",marginBottom:"1rem",display:"flex",alignItems:"center",gap:"0.5rem"},children:[r.jsx("span",{style:{fontSize:"1.25rem"},children:"⚠️"}),r.jsxs("div",{children:[r.jsx("strong",{style:{color:"var(--color-error)"},children:"Validation Error"}),r.jsx("div",{style:{fontSize:"0.875rem",marginTop:"0.25rem",color:"var(--color-text)"},children:e.email})]})]}),r.jsx("button",{type:"submit",children:"Submit"})]})]})}},A={render:()=>{const e=({title:t,errorMessage:n,children:d})=>r.jsxs("div",{style:{marginBottom:"1.5rem",padding:"1rem",border:`2px solid ${n?"var(--color-error)":"var(--color-border)"}`,borderRadius:"var(--radius-lg)",backgroundColor:n?"rgba(239, 68, 68, 0.05)":"var(--color-background-secondary)",transition:"all 0.2s ease"},children:[t&&r.jsxs("div",{style:{fontSize:"0.875rem",fontWeight:600,color:n?"var(--color-error)":"var(--color-text)",marginBottom:"0.75rem",display:"flex",alignItems:"center",gap:"0.5rem"},children:[n&&r.jsx("span",{children:"❌"}),!n&&r.jsx("span",{children:"✓"}),t]}),d,n&&r.jsx("div",{style:{marginTop:"0.5rem",fontSize:"0.875rem",color:"var(--color-error)",fontWeight:500},children:n})]});return r.jsxs(R,{size:"sm",asCard:!0,children:[r.jsx("h2",{children:"Custom Field Container"}),r.jsx("p",{children:"This form shows how to use a custom component for rendering field containers."}),r.jsxs(E,{command:q,fieldContainerComponent:e,children:[r.jsx(C,{value:t=>t.name,title:"Name",placeholder:"Enter your name (min 3 chars)"}),r.jsx(C,{value:t=>t.email,title:"Email",type:"email",placeholder:"Enter your email"}),r.jsx("button",{type:"submit",children:"Submit"})]})]})}};var G,J,K;k.parameters={...k.parameters,docs:{...(G=k.parameters)==null?void 0:G.docs,source:{originalSource:`{
  render: () => {
    const [validationState, setValidationState] = useState({
      errors: {},
      canSubmit: false
    });
    return _jsxs(StoryContainer, {
      size: "sm",
      asCard: true,
      children: [_jsx("h2", {
        children: "Simple Command Form with Validation"
      }), _jsx("p", {
        children: "This form demonstrates validation on blur. Fields are validated when you leave them."
      }), _jsxs(CommandForm, {
        command: SimpleCommand,
        initialValues: {
          name: '',
          email: ''
        },
        onFieldChange: async (command, fieldName) => {
          const result = await command.validate();
          if (!result.isValid) {
            const fieldError = result.validationResults.find(v => v.members.includes(fieldName));
            if (fieldError) {
              setValidationState(prev => ({
                errors: {
                  ...prev.errors,
                  [fieldName]: fieldError.message
                },
                canSubmit: false
              }));
            }
          } else {
            setValidationState(prev => {
              const {
                [fieldName]: removed,
                ...rest
              } = prev.errors;
              return {
                errors: rest,
                canSubmit: true
              };
            });
          }
        },
        children: [_jsx(InputTextField, {
          value: c => c.name,
          title: "Name",
          placeholder: "Enter your name (min 3 chars)"
        }), validationState.errors.name && _jsx("div", {
          style: {
            color: 'var(--color-error)',
            fontSize: '0.875rem',
            marginTop: '0.25rem',
            marginBottom: '1rem'
          },
          children: validationState.errors.name
        }), _jsx(InputTextField, {
          value: c => c.email,
          title: "Email",
          type: "email",
          placeholder: "Enter your email"
        }), validationState.errors.email && _jsx("div", {
          style: {
            color: 'var(--color-error)',
            fontSize: '0.875rem',
            marginTop: '0.25rem',
            marginBottom: '1rem'
          },
          children: validationState.errors.email
        }), _jsxs("div", {
          style: {
            marginTop: '1.5rem',
            display: 'flex',
            gap: '0.5rem',
            alignItems: 'center',
            flexWrap: 'wrap'
          },
          children: [_jsx("button", {
            type: "submit",
            disabled: !validationState.canSubmit,
            children: "Submit"
          }), !validationState.canSubmit && Object.keys(validationState.errors).length > 0 && _jsx(StoryBadge, {
            variant: "warning",
            children: "Please fix validation errors"
          })]
        })]
      })]
    });
  }
}`,...(K=(J=k.parameters)==null?void 0:J.docs)==null?void 0:K.source}}};var Q,X,Y;P.parameters={...P.parameters,docs:{...(Q=P.parameters)==null?void 0:Q.docs,source:{originalSource:`{
  render: () => {
    const [result, setResult] = useState('');
    const [canSubmit, setCanSubmit] = useState(false);
    const [validationSummary, setValidationSummary] = useState([]);
    const handleSubmit = () => {
      setResult('Form submitted successfully!');
    };
    return _jsxs(StoryContainer, {
      size: "sm",
      asCard: true,
      children: [_jsx("h1", {
        children: "User Registration Form"
      }), _jsx("p", {
        children: "This form validates progressively as you type. The submit button is enabled only when all validation passes."
      }), _jsxs(CommandForm, {
        command: UserRegistrationCommand,
        initialValues: {
          username: '',
          email: '',
          password: '',
          confirmPassword: '',
          age: 18,
          bio: '',
          favoriteColor: '#3b82f6',
          birthDate: '',
          agreeToTerms: false,
          experienceLevel: 50,
          role: ''
        },
        onFieldChange: async (command, fieldName, oldValue, newValue) => {
          console.log(\`Field \${fieldName} changed from\`, oldValue, 'to', newValue);
          const validationResult = await command.validate();
          if (!validationResult.isValid) {
            setValidationSummary(validationResult.validationResults.map(v => v.message));
            setCanSubmit(false);
          } else {
            setValidationSummary([]);
            setCanSubmit(true);
          }
        },
        children: [_jsx("h3", {
          children: "Account Information"
        }), _jsx(InputTextField, {
          value: c => c.username,
          title: "Username",
          placeholder: "Enter username"
        }), _jsx(InputTextField, {
          value: c => c.email,
          title: "Email Address",
          type: "email",
          placeholder: "Enter email"
        }), _jsx(InputTextField, {
          value: c => c.password,
          title: "Password",
          type: "password",
          placeholder: "Enter password"
        }), _jsx(InputTextField, {
          value: c => c.confirmPassword,
          title: "Confirm Password",
          type: "password",
          placeholder: "Confirm password"
        }), _jsx("h3", {
          style: {
            marginTop: 'var(--space-2xl)',
            marginBottom: 0
          },
          children: "Personal Information"
        }), _jsx(NumberField, {
          value: c => c.age,
          title: "Age",
          placeholder: "Enter age",
          min: 13,
          max: 120
        }), _jsx(InputTextField, {
          value: c => c.birthDate,
          title: "Birth Date",
          type: "date",
          placeholder: "Select birth date"
        }), _jsx(TextAreaField, {
          value: c => c.bio,
          title: "Bio",
          placeholder: "Tell us about yourself",
          rows: 4,
          required: false
        }), _jsx(InputTextField, {
          value: c => c.favoriteColor,
          title: "Favorite Color",
          type: "color"
        }), _jsx("h3", {
          style: {
            marginTop: 'var(--space-2xl)',
            marginBottom: 0
          },
          children: "Preferences"
        }), _jsx(SelectField, {
          value: c => c.role,
          title: "Role",
          options: roleOptions,
          optionIdField: "id",
          optionLabelField: "name",
          placeholder: "Select a role"
        }), _jsx(RangeField, {
          value: c => c.experienceLevel,
          title: "Experience Level",
          min: 0,
          max: 100,
          step: 10
        }), _jsx(CheckboxField, {
          value: c => c.agreeToTerms,
          label: "I agree to the terms and conditions"
        })]
      }), validationSummary.length > 0 && _jsxs("div", {
        className: "story-card",
        style: {
          backgroundColor: 'rgba(245, 158, 11, 0.1)',
          borderColor: 'var(--color-warning)',
          marginBottom: 'var(--space-lg)'
        },
        children: [_jsx("strong", {
          style: {
            color: 'var(--color-warning)'
          },
          children: "Validation Issues:"
        }), _jsx("ul", {
          style: {
            marginTop: 'var(--space-sm)',
            marginBottom: 0
          },
          children: validationSummary.map((error, index) => _jsx("li", {
            style: {
              color: 'var(--color-warning)'
            },
            children: error
          }, index))
        })]
      }), _jsxs("div", {
        style: {
          display: 'flex',
          gap: 'var(--space-md)',
          marginTop: 'var(--space-xl)',
          alignItems: 'center',
          flexWrap: 'wrap'
        },
        children: [_jsx("button", {
          onClick: handleSubmit,
          disabled: !canSubmit,
          style: {
            backgroundColor: canSubmit ? 'var(--color-success)' : undefined
          },
          children: "Submit"
        }), _jsx("button", {
          onClick: () => setResult(''),
          style: {
            backgroundColor: 'var(--color-text-muted)'
          },
          children: "Cancel"
        }), !canSubmit && _jsx(StoryBadge, {
          variant: "warning",
          children: "Complete required fields with valid data"
        })]
      }), result && _jsx("div", {
        className: "story-card",
        style: {
          backgroundColor: 'rgba(34, 197, 94, 0.1)',
          borderColor: 'var(--color-success)',
          marginTop: 'var(--space-lg)'
        },
        children: _jsx("p", {
          style: {
            color: 'var(--color-success)',
            fontWeight: 600,
            margin: 0
          },
          children: result
        })
      })]
    });
  }
}`,...(Y=(X=P.parameters)==null?void 0:X.docs)==null?void 0:Y.source}}};var ee,re,ne;B.parameters={...B.parameters,docs:{...(ee=B.parameters)==null?void 0:ee.docs,source:{originalSource:`{
  render: () => {
    return _jsxs(StoryContainer, {
      size: "sm",
      asCard: true,
      children: [_jsx("h2", {
        children: "Custom Titles"
      }), _jsx("p", {
        children: "This form shows how to disable built-in titles and use custom title rendering."
      }), _jsxs(CommandForm, {
        command: SimpleCommand,
        showTitles: false,
        children: [_jsxs("div", {
          style: {
            marginBottom: '1rem'
          },
          children: [_jsx("div", {
            style: {
              fontSize: '0.75rem',
              textTransform: 'uppercase',
              letterSpacing: '0.05em',
              marginBottom: '0.5rem',
              color: 'var(--color-text-secondary)',
              fontWeight: 600
            },
            children: "Full Name *"
          }), _jsx(InputTextField, {
            value: c => c.name,
            placeholder: "Enter your full name"
          })]
        }), _jsxs("div", {
          style: {
            marginBottom: '1rem'
          },
          children: [_jsx("div", {
            style: {
              fontSize: '0.875rem',
              marginBottom: '0.5rem',
              color: 'var(--color-primary)',
              fontWeight: 700
            },
            children: "\\uD83D\\uDCE7 Email Address"
          }), _jsx(InputTextField, {
            value: c => c.email,
            type: "email",
            placeholder: "your.email@example.com"
          })]
        }), _jsx("button", {
          type: "submit",
          children: "Submit"
        })]
      })]
    });
  }
}`,...(ne=(re=B.parameters)==null?void 0:re.docs)==null?void 0:ne.source}}};var te,ae,oe;z.parameters={...z.parameters,docs:{...(te=z.parameters)==null?void 0:te.docs,source:{originalSource:`{
  render: () => {
    const [errors, setErrors] = useState({});
    return _jsxs(StoryContainer, {
      size: "sm",
      asCard: true,
      children: [_jsx("h2", {
        children: "Custom Error Rendering"
      }), _jsx("p", {
        children: "This form shows how to disable built-in error messages and render custom ones."
      }), _jsxs(CommandForm, {
        command: SimpleCommand,
        showErrors: false,
        onFieldChange: async (command, fieldName) => {
          const result = await command.validate();
          if (!result.isValid) {
            const fieldError = result.validationResults.find(v => v.members.includes(fieldName));
            if (fieldError) {
              setErrors(prev => ({
                ...prev,
                [fieldName]: fieldError.message
              }));
            }
          } else {
            setErrors(prev => {
              const {
                [fieldName]: removed,
                ...rest
              } = prev;
              return rest;
            });
          }
        },
        children: [_jsx(InputTextField, {
          value: c => c.name,
          title: "Name",
          placeholder: "Enter your name (min 3 chars)"
        }), errors.name && _jsxs("div", {
          style: {
            backgroundColor: 'rgba(239, 68, 68, 0.1)',
            border: '1px solid var(--color-error)',
            borderRadius: 'var(--radius-md)',
            padding: '0.75rem',
            marginTop: '0.5rem',
            marginBottom: '1rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem'
          },
          children: [_jsx("span", {
            style: {
              fontSize: '1.25rem'
            },
            children: "\\u26A0\\uFE0F"
          }), _jsxs("div", {
            children: [_jsx("strong", {
              style: {
                color: 'var(--color-error)'
              },
              children: "Validation Error"
            }), _jsx("div", {
              style: {
                fontSize: '0.875rem',
                marginTop: '0.25rem',
                color: 'var(--color-text)'
              },
              children: errors.name
            })]
          })]
        }), _jsx(InputTextField, {
          value: c => c.email,
          title: "Email",
          type: "email",
          placeholder: "Enter your email"
        }), errors.email && _jsxs("div", {
          style: {
            backgroundColor: 'rgba(239, 68, 68, 0.1)',
            border: '1px solid var(--color-error)',
            borderRadius: 'var(--radius-md)',
            padding: '0.75rem',
            marginTop: '0.5rem',
            marginBottom: '1rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem'
          },
          children: [_jsx("span", {
            style: {
              fontSize: '1.25rem'
            },
            children: "\\u26A0\\uFE0F"
          }), _jsxs("div", {
            children: [_jsx("strong", {
              style: {
                color: 'var(--color-error)'
              },
              children: "Validation Error"
            }), _jsx("div", {
              style: {
                fontSize: '0.875rem',
                marginTop: '0.25rem',
                color: 'var(--color-text)'
              },
              children: errors.email
            })]
          })]
        }), _jsx("button", {
          type: "submit",
          children: "Submit"
        })]
      })]
    });
  }
}`,...(oe=(ae=z.parameters)==null?void 0:ae.docs)==null?void 0:oe.source}}};var se,ie,le;A.parameters={...A.parameters,docs:{...(se=A.parameters)==null?void 0:se.docs,source:{originalSource:`{
  render: () => {
    const CustomContainer = ({
      title,
      errorMessage,
      children
    }) => {
      return _jsxs("div", {
        style: {
          marginBottom: '1.5rem',
          padding: '1rem',
          border: \`2px solid \${errorMessage ? 'var(--color-error)' : 'var(--color-border)'}\`,
          borderRadius: 'var(--radius-lg)',
          backgroundColor: errorMessage ? 'rgba(239, 68, 68, 0.05)' : 'var(--color-background-secondary)',
          transition: 'all 0.2s ease'
        },
        children: [title && _jsxs("div", {
          style: {
            fontSize: '0.875rem',
            fontWeight: 600,
            color: errorMessage ? 'var(--color-error)' : 'var(--color-text)',
            marginBottom: '0.75rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem'
          },
          children: [errorMessage && _jsx("span", {
            children: "\\u274C"
          }), !errorMessage && _jsx("span", {
            children: "\\u2713"
          }), title]
        }), children, errorMessage && _jsx("div", {
          style: {
            marginTop: '0.5rem',
            fontSize: '0.875rem',
            color: 'var(--color-error)',
            fontWeight: 500
          },
          children: errorMessage
        })]
      });
    };
    return _jsxs(StoryContainer, {
      size: "sm",
      asCard: true,
      children: [_jsx("h2", {
        children: "Custom Field Container"
      }), _jsx("p", {
        children: "This form shows how to use a custom component for rendering field containers."
      }), _jsxs(CommandForm, {
        command: SimpleCommand,
        fieldContainerComponent: CustomContainer,
        children: [_jsx(InputTextField, {
          value: c => c.name,
          title: "Name",
          placeholder: "Enter your name (min 3 chars)"
        }), _jsx(InputTextField, {
          value: c => c.email,
          title: "Email",
          type: "email",
          placeholder: "Enter your email"
        }), _jsx("button", {
          type: "submit",
          children: "Submit"
        })]
      })]
    });
  }
}`,...(le=(ie=A.parameters)==null?void 0:ie.docs)==null?void 0:le.source}}};const $e=["Default","UserRegistration","CustomTitles","CustomErrorRendering","CustomFieldContainer"];export{z as CustomErrorRendering,A as CustomFieldContainer,B as CustomTitles,k as Default,P as UserRegistration,$e as __namedExportsOrder,Le as default};
