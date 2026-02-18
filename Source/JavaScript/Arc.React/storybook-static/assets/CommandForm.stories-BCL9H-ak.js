var we=Object.defineProperty;var Fe=(e,a,t)=>a in e?we(e,a,{enumerable:!0,configurable:!0,writable:!0,value:t}):e[a]=t;var f=(e,a,t)=>Fe(e,typeof a!="symbol"?a+"":a,t);import{j as r}from"./jsx-runtime-Cf8x2fCZ.js";import{r as p,R as C}from"./index-BO-NyGGJ.js";import{C as Se,G as Te,a as de,P as w,b as ce}from"./PropertyDescriptor-Bo5Iu_vo.js";import{S as N,c as ue}from"./StoryContainer-XwC7i3zG.js";import"./index-yBjzXJbu.js";const pe=p.createContext(void 0),ge=()=>{const e=p.useContext(pe);if(!e)throw new Error("useCommandFormContext must be used within a CommandForm");return e},A=({field:e})=>{var v;const a=ge(),t=e.props,m=t.value,n=m?W(m):"",i=C.useMemo(()=>{var c;return n?(c=a.commandInstance)==null?void 0:c[n]:void 0},[a.commandInstance,n,a.commandVersion]),l=n?a.getFieldError(n):void 0,o=n&&((v=a.commandInstance)!=null&&v.propertyDescriptors)?a.commandInstance.propertyDescriptors.find(c=>c.name===n):void 0,g=C.cloneElement(e,{...t,currentValue:i,propertyDescriptor:o,fieldName:n,onValueChange:c=>{var d;if(n){const u=i;if(a.setCommandValues({[n]:c}),a.commandInstance&&typeof a.commandInstance.validate=="function"){const y=a.commandInstance.validate();y&&a.setCommandResult(y)}if(a.onFieldValidate){const y=a.onFieldValidate(a.commandInstance,n,u,c);a.setCustomFieldError(n,y)}a.onFieldChange&&a.onFieldChange(a.commandInstance,n,u,c)}(d=t.onChange)==null||d.call(t,c)},required:t.required??!0,invalid:!!l}),h=a.fieldContainerComponent,s=r.jsxs(r.Fragment,{children:[a.showTitles&&t.title&&r.jsx("label",{style:{display:"block",marginBottom:"0.5rem",fontWeight:500,color:"var(--color-text)"},children:t.title}),r.jsxs("div",{style:{display:"flex",width:"100%"},children:[t.icon&&r.jsx("span",{title:t.description,style:{cursor:t.description?"help":"default",display:"flex",alignItems:"center",padding:"0.5rem",backgroundColor:"var(--color-background-secondary)",border:"1px solid var(--color-border)",borderRight:"none",borderRadius:"var(--radius-md) 0 0 var(--radius-md)"},children:t.icon}),g]}),a.showErrors&&l&&r.jsx("small",{style:{display:"block",marginTop:"0.25rem",color:"var(--color-error, #c00)",fontSize:"0.875rem"},children:l})]});return h?r.jsx(h,{title:t.title,errorMessage:l,children:s}):r.jsx("div",{className:"w-full",style:{marginBottom:"1rem"},children:s})};A.displayName="CommandFormFieldWrapper";const O=e=>{const{fields:a,columns:t,orderedChildren:m}=e;return t&&t.length>0?r.jsx("div",{className:"card flex flex-column md:flex-row gap-3",children:t.map((n,i)=>r.jsx("div",{className:"flex flex-column gap-3 flex-1",children:n.fields.map((l,o)=>{const h=l.props.value,s=h?W(h):`field-${i}-${o}`;return r.jsx(A,{field:l},s)})},`column-${i}`))}):m&&m.length>0?r.jsx("div",{style:{display:"flex",flexDirection:"column",width:"100%"},children:m.map(n=>{if(n.type==="field"){const i=n.content,o=i.props.value,g=o?W(o):`field-${n.index}`;return r.jsx(A,{field:i},g)}else return r.jsx(C.Fragment,{children:n.content},`other-${n.index}`)})}):r.jsx("div",{style:{display:"flex",flexDirection:"column",width:"100%"},children:(a||[]).map((n,i)=>{const o=n.props.value,g=o?W(o):`field-${i}`;return r.jsx(A,{field:n},g)})})};function W(e){const t=e.toString().match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);return t?t[1]:""}O.displayName="CommandFormFields";O.__docgenInfo={description:"",methods:[],displayName:"CommandFormFields",props:{fields:{required:!1,tsType:{name:"Array",elements:[{name:"ReactReactElement",raw:"React.ReactElement<CommandFormFieldProps>",elements:[{name:"CommandFormFieldProps"}]}],raw:"React.ReactElement<CommandFormFieldProps>[]"},description:""},columns:{required:!1,tsType:{name:"Array",elements:[{name:"ColumnInfo"}],raw:"ColumnInfo[]"},description:""},orderedChildren:{required:!1,tsType:{name:"Array",elements:[{name:"signature",type:"object",raw:"{ type: 'field' | 'other', content: React.ReactNode, index: number }",signature:{properties:[{key:"type",value:{name:"union",raw:"'field' | 'other'",elements:[{name:"literal",value:"'field'"},{name:"literal",value:"'other'"}],required:!0}},{key:"content",value:{name:"ReactReactNode",raw:"React.ReactNode",required:!0}},{key:"index",value:{name:"number",required:!0}}]}}],raw:"Array<{ type: 'field' | 'other', content: React.ReactNode, index: number }>"},description:""}}};const je={addCommand:()=>{},execute:async()=>new Se(new Map),hasChanges:!1,revertChanges:()=>{}},Ee=C.createContext(je),Ve=C.createContext({microservice:Te.microservice,development:!1,origin:"",basePath:"",apiBasePath:"",httpHeadersCallback:()=>({})});function he(e,a){var c;const t=p.useRef(null),[m,n]=p.useState(!1),[i,l]=p.useState(0),o=p.useContext(Ve),g=p.useCallback(()=>{var d,u;((d=t.current)==null?void 0:d.hasChanges)!==m&&n(((u=t.current)==null?void 0:u.hasChanges)??!1)},[]);t.current=p.useMemo(()=>{const d=new e;return d.setMicroservice(o.microservice),d.setApiBasePath(o.apiBasePath??""),d.setOrigin(o.origin??""),d.setHttpHeadersCallback(o.httpHeadersCallback??(()=>({}))),a&&d.setInitialValues(a),d.onPropertyChanged(g,d),d},[]);const h=C.useContext(Ee);(c=h.addCommand)==null||c.call(h,t.current);const s=p.useCallback(d=>{const u=d;t.current.propertyDescriptors.forEach(y=>{u[y.name]!==void 0&&u[y.name]!=null&&(t.current[y.name]=u[y.name])}),l(y=>y+1)},[]),v=p.useCallback(()=>{t.current.propertyDescriptors.forEach(d=>{t.current[d.name]=void 0}),l(d=>d+1)},[]);return[t.current,s,v,i]}function Re(e){if(typeof e!="function")return"";const t=e.toString().match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);return t?t[1]:""}const M=e=>(C.Children.forEach(e.children,a=>{if(C.isValidElement(a)){const t=a.type;if(t.displayName!=="CommandFormField")throw new Error(`Only CommandFormField components are allowed as children of CommandForm.Fields. Got: ${t.displayName||t.name||"Unknown"}`)}}),r.jsx(r.Fragment,{}));M.displayName="CommandFormFieldsWrapper";const ke=e=>{if(!e.children)return{fieldsOrColumns:[],otherChildren:[],initialValuesFromFields:{},orderedChildren:[]};let a=[];const t=[];let m=!1;const n=[],i=[];let l=0,o=0,g={};const h=s=>{const v=s.props;if(v.currentValue!==void 0&&v.value){const c=v.value,d=Re(c);d&&(g={...g,[d]:v.currentValue})}};return C.Children.toArray(e.children).forEach(s=>{if(!C.isValidElement(s)){n.push(s),i.push({type:"other",content:s,index:o++});return}const v=s.type;if(v.displayName==="CommandFormColumn"){m=!0;const c=s.props,d=C.Children.toArray(c.children).filter(u=>C.isValidElement(u)&&u.type.displayName==="CommandFormField"?(h(u),!0):!1);t.push({fields:d})}else if(v.displayName==="CommandFormField")h(s),a.push(s),i.push({type:"field",content:s,index:l++});else if(v===M||v.displayName==="CommandFormFieldsWrapper"){const c=s.props,d=C.Children.toArray(c.children).filter(u=>C.isValidElement(u)&&u.type.displayName==="CommandFormField"?(h(u),!0):!1);a=[...a,...d]}else n.push(s),i.push({type:"other",content:s,index:o++})}),{fieldsOrColumns:m?t:a,otherChildren:n,initialValuesFromFields:g,orderedChildren:i}},D=e=>{const{fieldsOrColumns:a,initialValuesFromFields:t,orderedChildren:m}=p.useMemo(()=>ke(e),[e.children]),n=p.useMemo(()=>{if(!e.currentValues)return{};const F=new e.command().properties||[],S={};return F.forEach(j=>{e.currentValues[j]!==void 0&&(S[j]=e.currentValues[j])}),S},[e.currentValues,e.command]),i=p.useMemo(()=>({...n,...t,...e.initialValues}),[n,t,e.initialValues]),l=he(e.command,i),o=l[0],g=l[1],h=l[3],[s,v]=p.useState(void 0),[c,d]=p.useState({}),[u,y]=p.useState({}),R=C.useRef(!1);C.useEffect(()=>{!R.current&&i&&Object.keys(i).length>0&&(g(i),R.current=!0)},[i]);const T=Object.values(c).every(b=>b),U=p.useCallback((b,F)=>{d(S=>({...S,[b]:F}))},[]),Z=p.useCallback((b,F)=>{y(S=>{if(F===void 0){const j={...S};return delete j[b],j}return{...S,[b]:F}})},[]),G=b=>{if(u[b])return u[b];if(!(!s||!s.validationResults)){for(const F of s.validationResults)if(F.members&&F.members.includes(b))return F.message}},I=(s==null?void 0:s.exceptionMessages)||[],k=a.length>0&&"fields"in a[0],$={command:e.command,commandInstance:o,commandVersion:h,setCommandValues:g,commandResult:s,setCommandResult:v,getFieldError:G,isValid:T,setFieldValidity:U,onFieldValidate:e.onFieldValidate,onFieldChange:e.onFieldChange,onBeforeExecute:e.onBeforeExecute,customFieldErrors:u,setCustomFieldError:Z,showTitles:e.showTitles??!0,showErrors:e.showErrors??!0,fieldContainerComponent:e.fieldContainerComponent};return r.jsxs(pe.Provider,{value:$,children:[r.jsx(O,{fields:k?void 0:a,columns:k?a:void 0,orderedChildren:m}),I.length>0&&r.jsxs("div",{style:{marginTop:"1rem",padding:"1rem",border:"1px solid var(--color-border)",borderRadius:"var(--radius-md)",backgroundColor:"var(--color-error-bg, #fee)"},children:[r.jsx("h4",{style:{margin:"0 0 0.5rem 0",fontSize:"1rem",fontWeight:600,color:"var(--color-error, #c00)"},children:"The server responded with"}),r.jsx("ul",{style:{margin:0,paddingLeft:"1.5rem"},children:I.map((b,F)=>r.jsx("li",{children:b},F))})]})]})},ve=e=>r.jsx(r.Fragment,{});ve.displayName="CommandFormColumn";D.Fields=M;D.Column=ve;const E=D;D.__docgenInfo={description:"",methods:[{name:"Fields",docblock:null,modifiers:["static"],params:[{name:"props",optional:!1,type:{name:"signature",type:"object",raw:"{ children: React.ReactNode }",signature:{properties:[{key:"children",value:{name:"ReactReactNode",raw:"React.ReactNode",required:!0}}]}}}],returns:null},{name:"Column",docblock:null,modifiers:["static"],params:[{name:"_props",optional:!1,type:{name:"signature",type:"object",raw:"{ children: React.ReactNode }",signature:{properties:[{key:"children",value:{name:"ReactReactNode",raw:"React.ReactNode",required:!0}}]}}}],returns:null}],displayName:"CommandFormComponent",props:{command:{required:!0,tsType:{name:"Constructor",elements:[{name:"TCommand"}],raw:"Constructor<TCommand>"},description:""},initialValues:{required:!1,tsType:{name:"Partial",elements:[{name:"TCommand"}],raw:"Partial<TCommand>"},description:""},currentValues:{required:!1,tsType:{name:"union",raw:"Partial<TCommand> | undefined",elements:[{name:"Partial",elements:[{name:"TCommand"}],raw:"Partial<TCommand>"},{name:"undefined"}]},description:""},onFieldValidate:{required:!1,tsType:{name:"signature",type:"function",raw:"(command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown) => string | undefined",signature:{arguments:[{type:{name:"TCommand"},name:"command"},{type:{name:"string"},name:"fieldName"},{type:{name:"unknown"},name:"oldValue"},{type:{name:"unknown"},name:"newValue"}],return:{name:"union",raw:"string | undefined",elements:[{name:"string"},{name:"undefined"}]}}},description:""},onFieldChange:{required:!1,tsType:{name:"signature",type:"function",raw:"(command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown) => void",signature:{arguments:[{type:{name:"TCommand"},name:"command"},{type:{name:"string"},name:"fieldName"},{type:{name:"unknown"},name:"oldValue"},{type:{name:"unknown"},name:"newValue"}],return:{name:"void"}}},description:""},onBeforeExecute:{required:!1,tsType:{name:"signature",type:"function",raw:"(values: TCommand) => TCommand",signature:{arguments:[{type:{name:"TCommand"},name:"values"}],return:{name:"TCommand"}}},description:""},showTitles:{required:!1,tsType:{name:"boolean"},description:""},showErrors:{required:!1,tsType:{name:"boolean"},description:""},fieldContainerComponent:{required:!1,tsType:{name:"ReactComponentType",raw:"React.ComponentType<import('./CommandFormContext').FieldContainerProps>",elements:[{name:"unknown"}]},description:""},children:{required:!1,tsType:{name:"ReactReactNode",raw:"React.ReactNode"},description:""}}};function V(e,a){var l;const{defaultValue:t,extractValue:m}=a,n=(typeof e=="function"&&!((l=e.prototype)!=null&&l.render),e),i=o=>{const{currentValue:g,onValueChange:h,fieldName:s,required:v=!0,...c}=o,{getFieldError:d,customFieldErrors:u}=ge(),y=s?d(s):void 0,R=s?u[s]:void 0,T=[];y&&T.push(y),R&&T.push(R);const U=T.length>0,I={...c,value:g!==void 0?g:t,onChange:k=>{const $=m?m(k):k;h==null||h($)},invalid:U,required:v,errors:T};return r.jsx(n,{...I})};return i.displayName="CommandFormField",i}const x=V(e=>r.jsx("input",{type:e.type||"text",value:e.value,onChange:e.onChange,required:e.required,placeholder:e.placeholder,className:`w-full p-3 rounded-md text-base ${e.invalid?"border border-red-500":"border border-gray-300"}`,style:{width:"100%",display:"block"}}),{defaultValue:"",extractValue:e=>e&&typeof e=="object"&&"target"in e?e.target.value:String(e||"")});x.__docgenInfo={description:"",methods:[],displayName:"InputTextField"};const fe=V(e=>r.jsx("input",{type:"number",value:e.value,onChange:e.onChange,required:e.required,placeholder:e.placeholder,min:e.min,max:e.max,step:e.step,className:`w-full p-3 rounded-md text-base ${e.invalid?"border border-red-500":"border border-gray-300"}`,style:{width:"100%",display:"block"}}),{defaultValue:0,extractValue:e=>e&&typeof e=="object"&&"target"in e?parseFloat(e.target.value)||0:typeof e=="number"?e:0});fe.__docgenInfo={description:"",methods:[],displayName:"NumberField"};const ye=V(e=>r.jsxs("div",{className:"flex items-center",children:[r.jsx("input",{type:"checkbox",checked:e.value,onChange:e.onChange,required:e.required,className:`h-5 w-5 rounded ${e.invalid?"border-red-500":"border-gray-300"}`}),e.label&&r.jsx("label",{className:"ml-2",children:e.label})]}),{defaultValue:!1,extractValue:e=>typeof e=="boolean"?e:e&&typeof e=="object"&&"target"in e?e.target.checked:!1});ye.__docgenInfo={description:"",methods:[],displayName:"CheckboxField"};const Ce=V(e=>r.jsx("textarea",{value:e.value,onChange:e.onChange,required:e.required,placeholder:e.placeholder,rows:e.rows??5,cols:e.cols,className:`w-full p-3 rounded-md text-base ${e.invalid?"border border-red-500":"border border-gray-300"}`,style:{width:"100%",display:"block"}}),{defaultValue:"",extractValue:e=>e&&typeof e=="object"&&"target"in e?e.target.value:String(e||"")});Ce.__docgenInfo={description:"",methods:[],displayName:"TextAreaField"};const xe=e=>r.jsxs("select",{value:e.value||"",onChange:e.onChange,required:e.required,className:`w-full p-3 rounded-md text-base ${e.invalid?"border border-red-500":"border border-gray-300"}`,style:{width:"100%",display:"block"},children:[e.placeholder&&r.jsx("option",{value:"",children:e.placeholder}),e.options.map((a,t)=>r.jsx("option",{value:String(a[e.optionIdField]),children:String(a[e.optionLabelField])},t))]}),Ne=V(xe,{defaultValue:"",extractValue:e=>e&&typeof e=="object"&&"target"in e?e.target.value:String(e)});xe.__docgenInfo={description:"",methods:[],displayName:"SelectComponent",props:{value:{required:!0,tsType:{name:"string"},description:""},onChange:{required:!0,tsType:{name:"signature",type:"function",raw:"(valueOrEvent: TValue | unknown) => void",signature:{arguments:[{type:{name:"union",raw:"TValue | unknown",elements:[{name:"string"},{name:"unknown"}]},name:"valueOrEvent"}],return:{name:"void"}}},description:""},invalid:{required:!0,tsType:{name:"boolean"},description:""},required:{required:!0,tsType:{name:"boolean"},description:""},errors:{required:!0,tsType:{name:"Array",elements:[{name:"string"}],raw:"string[]"},description:""},options:{required:!0,tsType:{name:"Array",elements:[{name:"signature",type:"object",raw:"{ [key: string]: unknown }",signature:{properties:[{key:{name:"string"},value:{name:"unknown",required:!0}}]}}],raw:"Array<{ [key: string]: unknown }>"},description:""},optionIdField:{required:!0,tsType:{name:"string"},description:""},optionLabelField:{required:!0,tsType:{name:"string"},description:""},placeholder:{required:!1,tsType:{name:"string"},description:""}}};const be=V(e=>{const a=e.min??0,t=e.max??100,m=e.step??1;return r.jsxs("div",{className:"w-full flex items-center gap-4 p-3 border border-gray-300 rounded-md",style:{display:"flex",alignItems:"center",gap:"1rem",padding:"0.75rem",border:"1px solid var(--color-border)",borderRadius:"0.375rem",backgroundColor:"var(--color-background-secondary)"},children:[r.jsx("input",{type:"range",value:e.value,onChange:e.onChange,min:a,max:t,step:m,required:e.required,className:"flex-1",style:{flex:1}}),r.jsx("span",{className:"min-w-[3rem] text-right font-semibold",style:{minWidth:"3rem",textAlign:"right",fontWeight:600,color:"var(--color-text)"},children:e.value})]})},{defaultValue:0,extractValue:e=>e&&typeof e=="object"&&"target"in e?parseFloat(e.target.value):typeof e=="number"?e:typeof e=="string"?parseFloat(e):0});be.__docgenInfo={description:"",methods:[],displayName:"RangeField"};class Ie extends ce{constructor(){super(),this.ruleFor(a=>a.username).notEmpty().minLength(3).maxLength(20),this.ruleFor(a=>a.email).notEmpty().emailAddress(),this.ruleFor(a=>a.password).notEmpty().minLength(8),this.ruleFor(a=>a.age).greaterThanOrEqual(13).lessThanOrEqual(120)}}class H extends de{constructor(){super(Object,!1);f(this,"route","/api/users/register");f(this,"validation",new Ie);f(this,"propertyDescriptors",[new w("username",String),new w("email",String),new w("password",String),new w("confirmPassword",String),new w("age",Number),new w("bio",String),new w("favoriteColor",String),new w("birthDate",String),new w("agreeToTerms",Boolean),new w("experienceLevel",Number),new w("role",String)]);f(this,"_username");f(this,"_email");f(this,"_password");f(this,"_confirmPassword");f(this,"_age");f(this,"_bio");f(this,"_favoriteColor");f(this,"_birthDate");f(this,"_agreeToTerms");f(this,"_experienceLevel");f(this,"_role")}get requestParameters(){return[]}get properties(){return["username","email","password","confirmPassword","age","bio","favoriteColor","birthDate","agreeToTerms","experienceLevel","role"]}get username(){return this._username}set username(t){this._username=t,this.propertyChanged("username")}get email(){return this._email}set email(t){this._email=t,this.propertyChanged("email")}get password(){return this._password}set password(t){this._password=t,this.propertyChanged("password")}get confirmPassword(){return this._confirmPassword}set confirmPassword(t){this._confirmPassword=t,this.propertyChanged("confirmPassword")}get age(){return this._age}set age(t){this._age=t,this.propertyChanged("age")}get bio(){return this._bio}set bio(t){this._bio=t,this.propertyChanged("bio")}get favoriteColor(){return this._favoriteColor}set favoriteColor(t){this._favoriteColor=t,this.propertyChanged("favoriteColor")}get birthDate(){return this._birthDate}set birthDate(t){this._birthDate=t,this.propertyChanged("birthDate")}get agreeToTerms(){return this._agreeToTerms}set agreeToTerms(t){this._agreeToTerms=t,this.propertyChanged("agreeToTerms")}get experienceLevel(){return this._experienceLevel}set experienceLevel(t){this._experienceLevel=t,this.propertyChanged("experienceLevel")}get role(){return this._role}set role(t){this._role=t,this.propertyChanged("role")}static use(t){return he(H,t)}}const Le={title:"CommandForm/CommandForm",component:E};class L extends de{constructor(){super(Object,!1);f(this,"route","/api/simple");f(this,"validation",new Pe);f(this,"propertyDescriptors",[new w("name",String),new w("email",String)]);f(this,"name","");f(this,"email","")}get requestParameters(){return[]}get properties(){return["name","email"]}}class Pe extends ce{constructor(){super(),this.ruleFor(a=>a.name).notEmpty().minLength(3),this.ruleFor(a=>a.email).notEmpty().emailAddress()}}const _e=[{id:"user",name:"User"},{id:"admin",name:"Administrator"},{id:"moderator",name:"Moderator"}],P={render:()=>{const[e,a]=p.useState({errors:{},canSubmit:!1});return r.jsxs(N,{size:"sm",asCard:!0,children:[r.jsx("h2",{children:"Simple Command Form with Validation"}),r.jsx("p",{children:"This form demonstrates validation on blur. Fields are validated when you leave them."}),r.jsxs(E,{command:L,initialValues:{name:"",email:""},onFieldChange:async(t,m)=>{const n=await t.validate();if(n.isValid)a(i=>{const{[m]:l,...o}=i.errors;return{errors:o,canSubmit:!0}});else{const i=n.validationResults.find(l=>l.members.includes(m));i&&a(l=>({errors:{...l.errors,[m]:i.message},canSubmit:!1}))}},children:[r.jsx(x,{value:t=>t.name,title:"Name",placeholder:"Enter your name (min 3 chars)"}),e.errors.name&&r.jsx("div",{style:{color:"var(--color-error)",fontSize:"0.875rem",marginTop:"0.25rem",marginBottom:"1rem"},children:e.errors.name}),r.jsx(x,{value:t=>t.email,title:"Email",type:"email",placeholder:"Enter your email"}),e.errors.email&&r.jsx("div",{style:{color:"var(--color-error)",fontSize:"0.875rem",marginTop:"0.25rem",marginBottom:"1rem"},children:e.errors.email}),r.jsxs("div",{style:{marginTop:"1.5rem",display:"flex",gap:"0.5rem",alignItems:"center",flexWrap:"wrap"},children:[r.jsx("button",{type:"submit",disabled:!e.canSubmit,children:"Submit"}),!e.canSubmit&&Object.keys(e.errors).length>0&&r.jsx(ue,{variant:"warning",children:"Please fix validation errors"})]})]})]})}},_={render:()=>{const[e,a]=p.useState(""),[t,m]=p.useState(!1),[n,i]=p.useState([]),l=()=>{a("Form submitted successfully!")};return r.jsxs(N,{size:"sm",asCard:!0,children:[r.jsx("h1",{children:"User Registration Form"}),r.jsx("p",{children:"This form validates progressively as you type. The submit button is enabled only when all validation passes."}),r.jsxs(E,{command:H,initialValues:{username:"",email:"",password:"",confirmPassword:"",age:18,bio:"",favoriteColor:"#3b82f6",birthDate:"",agreeToTerms:!1,experienceLevel:50,role:""},onFieldChange:async(o,g,h,s)=>{console.log(`Field ${g} changed from`,h,"to",s);const v=await o.validate();v.isValid?(i([]),m(!0)):(i(v.validationResults.map(c=>c.message)),m(!1))},children:[r.jsx("h3",{children:"Account Information"}),r.jsx(x,{value:o=>o.username,title:"Username",placeholder:"Enter username"}),r.jsx(x,{value:o=>o.email,title:"Email Address",type:"email",placeholder:"Enter email"}),r.jsx(x,{value:o=>o.password,title:"Password",type:"password",placeholder:"Enter password"}),r.jsx(x,{value:o=>o.confirmPassword,title:"Confirm Password",type:"password",placeholder:"Confirm password"}),r.jsx("h3",{style:{marginTop:"var(--space-2xl)",marginBottom:0},children:"Personal Information"}),r.jsx(fe,{value:o=>o.age,title:"Age",placeholder:"Enter age",min:13,max:120}),r.jsx(x,{value:o=>o.birthDate,title:"Birth Date",type:"date",placeholder:"Select birth date"}),r.jsx(Ce,{value:o=>o.bio,title:"Bio",placeholder:"Tell us about yourself",rows:4,required:!1}),r.jsx(x,{value:o=>o.favoriteColor,title:"Favorite Color",type:"color"}),r.jsx("h3",{style:{marginTop:"var(--space-2xl)",marginBottom:0},children:"Preferences"}),r.jsx(Ne,{value:o=>o.role,title:"Role",options:_e,optionIdField:"id",optionLabelField:"name",placeholder:"Select a role"}),r.jsx(be,{value:o=>o.experienceLevel,title:"Experience Level",min:0,max:100,step:10}),r.jsx(ye,{value:o=>o.agreeToTerms,label:"I agree to the terms and conditions"})]}),n.length>0&&r.jsxs("div",{className:"story-card",style:{backgroundColor:"rgba(245, 158, 11, 0.1)",borderColor:"var(--color-warning)",marginBottom:"var(--space-lg)"},children:[r.jsx("strong",{style:{color:"var(--color-warning)"},children:"Validation Issues:"}),r.jsx("ul",{style:{marginTop:"var(--space-sm)",marginBottom:0},children:n.map((o,g)=>r.jsx("li",{style:{color:"var(--color-warning)"},children:o},g))})]}),r.jsxs("div",{style:{display:"flex",gap:"var(--space-md)",marginTop:"var(--space-xl)",alignItems:"center",flexWrap:"wrap"},children:[r.jsx("button",{onClick:l,disabled:!t,style:{backgroundColor:t?"var(--color-success)":void 0},children:"Submit"}),r.jsx("button",{onClick:()=>a(""),style:{backgroundColor:"var(--color-text-muted)"},children:"Cancel"}),!t&&r.jsx(ue,{variant:"warning",children:"Complete required fields with valid data"})]}),e&&r.jsx("div",{className:"story-card",style:{backgroundColor:"rgba(34, 197, 94, 0.1)",borderColor:"var(--color-success)",marginTop:"var(--space-lg)"},children:r.jsx("p",{style:{color:"var(--color-success)",fontWeight:600,margin:0},children:e})})]})}},q={render:()=>r.jsxs(N,{size:"sm",asCard:!0,children:[r.jsx("h2",{children:"Custom Titles"}),r.jsx("p",{children:"This form shows how to disable built-in titles and use custom title rendering."}),r.jsxs(E,{command:L,showTitles:!1,children:[r.jsxs("div",{style:{marginBottom:"1rem"},children:[r.jsx("div",{style:{fontSize:"0.75rem",textTransform:"uppercase",letterSpacing:"0.05em",marginBottom:"0.5rem",color:"var(--color-text-secondary)",fontWeight:600},children:"Full Name *"}),r.jsx(x,{value:e=>e.name,placeholder:"Enter your full name"})]}),r.jsxs("div",{style:{marginBottom:"1rem"},children:[r.jsx("div",{style:{fontSize:"0.875rem",marginBottom:"0.5rem",color:"var(--color-primary)",fontWeight:700},children:"📧 Email Address"}),r.jsx(x,{value:e=>e.email,type:"email",placeholder:"your.email@example.com"})]}),r.jsx("button",{type:"submit",children:"Submit"})]})]})},B={render:()=>{const[e,a]=p.useState({});return r.jsxs(N,{size:"sm",asCard:!0,children:[r.jsx("h2",{children:"Custom Error Rendering"}),r.jsx("p",{children:"This form shows how to disable built-in error messages and render custom ones."}),r.jsxs(E,{command:L,showErrors:!1,onFieldChange:async(t,m)=>{const n=await t.validate();if(n.isValid)a(i=>{const{[m]:l,...o}=i;return o});else{const i=n.validationResults.find(l=>l.members.includes(m));i&&a(l=>({...l,[m]:i.message}))}},children:[r.jsx(x,{value:t=>t.name,title:"Name",placeholder:"Enter your name (min 3 chars)"}),e.name&&r.jsxs("div",{style:{backgroundColor:"rgba(239, 68, 68, 0.1)",border:"1px solid var(--color-error)",borderRadius:"var(--radius-md)",padding:"0.75rem",marginTop:"0.5rem",marginBottom:"1rem",display:"flex",alignItems:"center",gap:"0.5rem"},children:[r.jsx("span",{style:{fontSize:"1.25rem"},children:"⚠️"}),r.jsxs("div",{children:[r.jsx("strong",{style:{color:"var(--color-error)"},children:"Validation Error"}),r.jsx("div",{style:{fontSize:"0.875rem",marginTop:"0.25rem",color:"var(--color-text)"},children:e.name})]})]}),r.jsx(x,{value:t=>t.email,title:"Email",type:"email",placeholder:"Enter your email"}),e.email&&r.jsxs("div",{style:{backgroundColor:"rgba(239, 68, 68, 0.1)",border:"1px solid var(--color-error)",borderRadius:"var(--radius-md)",padding:"0.75rem",marginTop:"0.5rem",marginBottom:"1rem",display:"flex",alignItems:"center",gap:"0.5rem"},children:[r.jsx("span",{style:{fontSize:"1.25rem"},children:"⚠️"}),r.jsxs("div",{children:[r.jsx("strong",{style:{color:"var(--color-error)"},children:"Validation Error"}),r.jsx("div",{style:{fontSize:"0.875rem",marginTop:"0.25rem",color:"var(--color-text)"},children:e.email})]})]}),r.jsx("button",{type:"submit",children:"Submit"})]})]})}},z={render:()=>{const e=({title:a,errorMessage:t,children:m})=>r.jsxs("div",{style:{marginBottom:"1.5rem",padding:"1rem",border:`2px solid ${t?"var(--color-error)":"var(--color-border)"}`,borderRadius:"var(--radius-lg)",backgroundColor:t?"rgba(239, 68, 68, 0.05)":"var(--color-background-secondary)",transition:"all 0.2s ease"},children:[a&&r.jsxs("div",{style:{fontSize:"0.875rem",fontWeight:600,color:t?"var(--color-error)":"var(--color-text)",marginBottom:"0.75rem",display:"flex",alignItems:"center",gap:"0.5rem"},children:[t&&r.jsx("span",{children:"❌"}),!t&&r.jsx("span",{children:"✓"}),a]}),m,t&&r.jsx("div",{style:{marginTop:"0.5rem",fontSize:"0.875rem",color:"var(--color-error)",fontWeight:500},children:t})]});return r.jsxs(N,{size:"sm",asCard:!0,children:[r.jsx("h2",{children:"Custom Field Container"}),r.jsx("p",{children:"This form shows how to use a custom component for rendering field containers."}),r.jsxs(E,{command:L,fieldContainerComponent:e,children:[r.jsx(x,{value:a=>a.name,title:"Name",placeholder:"Enter your name (min 3 chars)"}),r.jsx(x,{value:a=>a.email,title:"Email",type:"email",placeholder:"Enter your email"}),r.jsx("button",{type:"submit",children:"Submit"})]})]})}};var J,K,Q;P.parameters={...P.parameters,docs:{...(J=P.parameters)==null?void 0:J.docs,source:{originalSource:`{
  render: () => {
    const [validationState, setValidationState] = useState<{
      errors: Record<string, string>;
      canSubmit: boolean;
    }>({
      errors: {},
      canSubmit: false
    });
    return <StoryContainer size="sm" asCard>
                <h2>Simple Command Form with Validation</h2>
                <p>
                    This form demonstrates validation on blur. Fields are validated when you leave them.
                </p>
                <CommandForm<SimpleCommand> command={SimpleCommand} initialValues={{
        name: '',
        email: ''
      }} onFieldChange={async (command, fieldName) => {
        // Validate on blur pattern
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
      }}>
                    <InputTextField<SimpleCommand> value={c => c.name} title="Name" placeholder="Enter your name (min 3 chars)" />
                    {validationState.errors.name && <div style={{
          color: 'var(--color-error)',
          fontSize: '0.875rem',
          marginTop: '0.25rem',
          marginBottom: '1rem'
        }}>
                            {validationState.errors.name}
                        </div>}
                    
                    <InputTextField<SimpleCommand> value={c => c.email} title="Email" type="email" placeholder="Enter your email" />
                    {validationState.errors.email && <div style={{
          color: 'var(--color-error)',
          fontSize: '0.875rem',
          marginTop: '0.25rem',
          marginBottom: '1rem'
        }}>
                            {validationState.errors.email}
                        </div>}

                    <div style={{
          marginTop: '1.5rem',
          display: 'flex',
          gap: '0.5rem',
          alignItems: 'center',
          flexWrap: 'wrap'
        }}>
                        <button type="submit" disabled={!validationState.canSubmit}>
                            Submit
                        </button>
                        {!validationState.canSubmit && Object.keys(validationState.errors).length > 0 && <StoryBadge variant="warning">Please fix validation errors</StoryBadge>}
                    </div>
                </CommandForm>
            </StoryContainer>;
  }
}`,...(Q=(K=P.parameters)==null?void 0:K.docs)==null?void 0:Q.source}}};var X,Y,ee;_.parameters={..._.parameters,docs:{...(X=_.parameters)==null?void 0:X.docs,source:{originalSource:`{
  render: () => {
    const [result, setResult] = useState<string>('');
    const [canSubmit, setCanSubmit] = useState(false);
    const [validationSummary, setValidationSummary] = useState<string[]>([]);
    const handleSubmit = () => {
      setResult('Form submitted successfully!');
    };
    return <StoryContainer size="sm" asCard>
                <h1>User Registration Form</h1>
                <p>
                    This form validates progressively as you type. The submit button is enabled only when all validation passes.
                </p>
                               
                <CommandForm<UserRegistrationCommand> command={UserRegistrationCommand} initialValues={{
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
      }} onFieldChange={async (command, fieldName, oldValue, newValue) => {
        console.log(\`Field \${fieldName} changed from\`, oldValue, 'to', newValue);

        // Progressive validation - validate whenever fields change
        const validationResult = await command.validate();
        if (!validationResult.isValid) {
          setValidationSummary(validationResult.validationResults.map(v => v.message));
          setCanSubmit(false);
        } else {
          setValidationSummary([]);
          setCanSubmit(true);
        }
      }}>
                    <h3>Account Information</h3>
                    <InputTextField<UserRegistrationCommand> value={c => c.username} title="Username" placeholder="Enter username" />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.email} title="Email Address" type="email" placeholder="Enter email" />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.password} title="Password" type="password" placeholder="Enter password" />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.confirmPassword} title="Confirm Password" type="password" placeholder="Confirm password" />

                    <h3 style={{
          marginTop: 'var(--space-2xl)',
          marginBottom: 0
        }}>Personal Information</h3>
                    
                    <NumberField<UserRegistrationCommand> value={c => c.age} title="Age" placeholder="Enter age" min={13} max={120} />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.birthDate} title="Birth Date" type="date" placeholder="Select birth date" />
                    
                    <TextAreaField<UserRegistrationCommand> value={c => c.bio} title="Bio" placeholder="Tell us about yourself" rows={4} required={false} />
                    
                    <InputTextField<UserRegistrationCommand> value={c => c.favoriteColor} title="Favorite Color" type="color" />

                    <h3 style={{
          marginTop: 'var(--space-2xl)',
          marginBottom: 0
        }}>Preferences</h3>
                    
                    <SelectField<UserRegistrationCommand> value={c => c.role} title="Role" options={roleOptions} optionIdField="id" optionLabelField="name" placeholder="Select a role" />
                    
                    <RangeField<UserRegistrationCommand> value={c => c.experienceLevel} title="Experience Level" min={0} max={100} step={10} />
                    
                    <CheckboxField<UserRegistrationCommand> value={c => c.agreeToTerms} label="I agree to the terms and conditions" />
                </CommandForm>

                {validationSummary.length > 0 && <div className="story-card" style={{
        backgroundColor: 'rgba(245, 158, 11, 0.1)',
        borderColor: 'var(--color-warning)',
        marginBottom: 'var(--space-lg)'
      }}>
                        <strong style={{
          color: 'var(--color-warning)'
        }}>Validation Issues:</strong>
                        <ul style={{
          marginTop: 'var(--space-sm)',
          marginBottom: 0
        }}>
                            {validationSummary.map((error, index) => <li key={index} style={{
            color: 'var(--color-warning)'
          }}>{error}</li>)}
                        </ul>
                    </div>}

                <div style={{
        display: 'flex',
        gap: 'var(--space-md)',
        marginTop: 'var(--space-xl)',
        alignItems: 'center',
        flexWrap: 'wrap'
      }}>
                    <button onClick={handleSubmit} disabled={!canSubmit} style={{
          backgroundColor: canSubmit ? 'var(--color-success)' : undefined
        }}>
                        Submit
                    </button>
                    <button onClick={() => setResult('')} style={{
          backgroundColor: 'var(--color-text-muted)'
        }}>
                        Cancel
                    </button>
                    {!canSubmit && <StoryBadge variant="warning">Complete required fields with valid data</StoryBadge>}
                </div>

                {result && <div className="story-card" style={{
        backgroundColor: 'rgba(34, 197, 94, 0.1)',
        borderColor: 'var(--color-success)',
        marginTop: 'var(--space-lg)'
      }}>
                        <p style={{
          color: 'var(--color-success)',
          fontWeight: 600,
          margin: 0
        }}>{result}</p>
                    </div>}
            </StoryContainer>;
  }
}`,...(ee=(Y=_.parameters)==null?void 0:Y.docs)==null?void 0:ee.source}}};var re,te,ae;q.parameters={...q.parameters,docs:{...(re=q.parameters)==null?void 0:re.docs,source:{originalSource:`{
  render: () => {
    return <StoryContainer size="sm" asCard>
                <h2>Custom Titles</h2>
                <p>
                    This form shows how to disable built-in titles and use custom title rendering.
                </p>
                <CommandForm<SimpleCommand> command={SimpleCommand} showTitles={false}>
                    <div style={{
          marginBottom: '1rem'
        }}>
                        <div style={{
            fontSize: '0.75rem',
            textTransform: 'uppercase',
            letterSpacing: '0.05em',
            marginBottom: '0.5rem',
            color: 'var(--color-text-secondary)',
            fontWeight: 600
          }}>
                            Full Name *
                        </div>
                        <InputTextField<SimpleCommand> value={c => c.name} placeholder="Enter your full name" />
                    </div>

                    <div style={{
          marginBottom: '1rem'
        }}>
                        <div style={{
            fontSize: '0.875rem',
            marginBottom: '0.5rem',
            color: 'var(--color-primary)',
            fontWeight: 700
          }}>
                            📧 Email Address
                        </div>
                        <InputTextField<SimpleCommand> value={c => c.email} type="email" placeholder="your.email@example.com" />
                    </div>

                    <button type="submit">Submit</button>
                </CommandForm>
            </StoryContainer>;
  }
}`,...(ae=(te=q.parameters)==null?void 0:te.docs)==null?void 0:ae.source}}};var oe,ne,ie;B.parameters={...B.parameters,docs:{...(oe=B.parameters)==null?void 0:oe.docs,source:{originalSource:`{
  render: () => {
    const [errors, setErrors] = useState<Record<string, string>>({});
    return <StoryContainer size="sm" asCard>
                <h2>Custom Error Rendering</h2>
                <p>
                    This form shows how to disable built-in error messages and render custom ones.
                </p>
                <CommandForm<SimpleCommand> command={SimpleCommand} showErrors={false} onFieldChange={async (command, fieldName) => {
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
      }}>
                    <InputTextField<SimpleCommand> value={c => c.name} title="Name" placeholder="Enter your name (min 3 chars)" />
                    {errors.name && <div style={{
          backgroundColor: 'rgba(239, 68, 68, 0.1)',
          border: '1px solid var(--color-error)',
          borderRadius: 'var(--radius-md)',
          padding: '0.75rem',
          marginTop: '0.5rem',
          marginBottom: '1rem',
          display: 'flex',
          alignItems: 'center',
          gap: '0.5rem'
        }}>
                            <span style={{
            fontSize: '1.25rem'
          }}>⚠️</span>
                            <div>
                                <strong style={{
              color: 'var(--color-error)'
            }}>Validation Error</strong>
                                <div style={{
              fontSize: '0.875rem',
              marginTop: '0.25rem',
              color: 'var(--color-text)'
            }}>
                                    {errors.name}
                                </div>
                            </div>
                        </div>}
                    
                    <InputTextField<SimpleCommand> value={c => c.email} title="Email" type="email" placeholder="Enter your email" />
                    {errors.email && <div style={{
          backgroundColor: 'rgba(239, 68, 68, 0.1)',
          border: '1px solid var(--color-error)',
          borderRadius: 'var(--radius-md)',
          padding: '0.75rem',
          marginTop: '0.5rem',
          marginBottom: '1rem',
          display: 'flex',
          alignItems: 'center',
          gap: '0.5rem'
        }}>
                            <span style={{
            fontSize: '1.25rem'
          }}>⚠️</span>
                            <div>
                                <strong style={{
              color: 'var(--color-error)'
            }}>Validation Error</strong>
                                <div style={{
              fontSize: '0.875rem',
              marginTop: '0.25rem',
              color: 'var(--color-text)'
            }}>
                                    {errors.email}
                                </div>
                            </div>
                        </div>}

                    <button type="submit">Submit</button>
                </CommandForm>
            </StoryContainer>;
  }
}`,...(ie=(ne=B.parameters)==null?void 0:ne.docs)==null?void 0:ie.source}}};var se,le,me;z.parameters={...z.parameters,docs:{...(se=z.parameters)==null?void 0:se.docs,source:{originalSource:`{
  render: () => {
    const CustomContainer: React.FC<import('./CommandFormContext').FieldContainerProps> = ({
      title,
      errorMessage,
      children
    }) => {
      return <div style={{
        marginBottom: '1.5rem',
        padding: '1rem',
        border: \`2px solid \${errorMessage ? 'var(--color-error)' : 'var(--color-border)'}\`,
        borderRadius: 'var(--radius-lg)',
        backgroundColor: errorMessage ? 'rgba(239, 68, 68, 0.05)' : 'var(--color-background-secondary)',
        transition: 'all 0.2s ease'
      }}>
                    {title && <div style={{
          fontSize: '0.875rem',
          fontWeight: 600,
          color: errorMessage ? 'var(--color-error)' : 'var(--color-text)',
          marginBottom: '0.75rem',
          display: 'flex',
          alignItems: 'center',
          gap: '0.5rem'
        }}>
                            {errorMessage && <span>❌</span>}
                            {!errorMessage && <span>✓</span>}
                            {title}
                        </div>}
                    {children}
                    {errorMessage && <div style={{
          marginTop: '0.5rem',
          fontSize: '0.875rem',
          color: 'var(--color-error)',
          fontWeight: 500
        }}>
                            {errorMessage}
                        </div>}
                </div>;
    };
    return <StoryContainer size="sm" asCard>
                <h2>Custom Field Container</h2>
                <p>
                    This form shows how to use a custom component for rendering field containers.
                </p>
                <CommandForm<SimpleCommand> command={SimpleCommand} fieldContainerComponent={CustomContainer}>
                    <InputTextField<SimpleCommand> value={c => c.name} title="Name" placeholder="Enter your name (min 3 chars)" />
                    
                    <InputTextField<SimpleCommand> value={c => c.email} title="Email" type="email" placeholder="Enter your email" />

                    <button type="submit">Submit</button>
                </CommandForm>
            </StoryContainer>;
  }
}`,...(me=(le=z.parameters)==null?void 0:le.docs)==null?void 0:me.source}}};const Ue=["Default","UserRegistration","CustomTitles","CustomErrorRendering","CustomFieldContainer"];export{B as CustomErrorRendering,z as CustomFieldContainer,q as CustomTitles,P as Default,_ as UserRegistration,Ue as __namedExportsOrder,Le as default};
